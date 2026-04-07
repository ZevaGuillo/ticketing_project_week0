// HUMAN CHECK: Completar lógica de waitlist cuando reserva expire.
using System.Text.Json;
using Confluent.Kafka;
using Inventory.Domain.Enums;
using Inventory.Domain.Events;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging;

public class ReservationExpiredEventConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpiredEventConsumer> _logger;
    private readonly IConsumer<string?, string> _consumer;
    private readonly IProducer<string?, string> _dlqProducer;
    private readonly WaitlistSettings _waitlistSettings;
    private readonly string _topic = "reservation-expired";
    private readonly string _dlqTopic = "reservation-expired-dlq";
    private readonly string _waitlistTopic = "waitlist-opportunity";

    public ReservationExpiredEventConsumer(
        IServiceScopeFactory scopeFactory,
        IConsumer<string?, string> consumer,
        IProducer<string?, string> dlqProducer,
        ILogger<ReservationExpiredEventConsumer> logger,
        WaitlistSettings waitlistSettings)
    {
        _scopeFactory = scopeFactory;
        _consumer = consumer;
        _dlqProducer = dlqProducer;
        _logger = logger;
        _waitlistSettings = waitlistSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationExpiredEventConsumer starting...");

        await Task.Delay(5000, stoppingToken);

        _consumer.Subscribe(_topic);
        _logger.LogInformation("Subscribed to topic {Topic}", _topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(TimeSpan.FromSeconds(10));
                    if (result == null || result.IsPartitionEOF) continue;

                    _logger.LogInformation("Received reservation-expired event: {Partition} {Offset}", 
                        result.Partition, result.Offset);

                    var reservationExpired = JsonSerializer.Deserialize<ReservationExpiredEvent>(
                        result.Message.Value,
                        CaseInsensitiveOptions);

                    if (reservationExpired != null)
                    {
                        await ProcessEventAsync(reservationExpired, stoppingToken);
                        _consumer.Commit(result);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                    await SendToDlqAsync(ex.Message, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing reservation-expired event");
                    await SendToDlqAsync(ex.Message, stoppingToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task SendToDlqAsync(string errorMessage, CancellationToken _)
    {
        try
        {
            _logger.LogWarning("Sending failed message to DLQ: {Topic}, Error: {Error}", _dlqTopic, errorMessage);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to DLQ");
        }
    }

    public async Task ProcessEventAsync(ReservationExpiredEvent evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var redisConfig = scope.ServiceProvider.GetService<WaitlistRedisConfiguration>();
        var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();

        _logger.LogInformation(
            "Processing reservation expired: {ReservationId}, Seat: {SeatId}, Event: {EventId}, Section: {Section}",
            evt.ReservationId, evt.SeatId, evt.EventId, evt.Section);

        // 1. Get available seat from inventory
        var availableSeat = await dbContext.Seats
            .FirstOrDefaultAsync(s => s.Id == evt.SeatId, ct);

        if (availableSeat == null)
        {
            _logger.LogWarning("Seat {SeatId} not found in inventory, skipping waitlist processing", evt.SeatId);
            return;
        }

        if (!availableSeat.Reserved)
        {
            _logger.LogInformation("Seat {SeatId} is not reserved (possibly already sold), skipping waitlist", evt.SeatId);
            return;
        }

        // 2. Release the seat (mark as not reserved)
        availableSeat.Reserved = false;
        
        // 3. Find active waitlist entries for this event and section
        var waitlistEntries = await dbContext.WaitlistEntries
            .Where(w => w.EventId == evt.EventId 
                        && w.Section == evt.Section 
                        && w.Status == WaitlistStatus.ACTIVE)
            .OrderBy(w => w.JoinedAt)
            .ToListAsync(ct);

        if (!waitlistEntries.Any())
        {
            _logger.LogInformation(
                "No active waitlist entries for Event {EventId}, Section {Section}, keeping seat available",
                evt.EventId, evt.Section);
            await dbContext.SaveChangesAsync(ct);
            return;
        }

        _logger.LogInformation("Found {Count} waitlist entries for Event {EventId}, Section {Section}",
            waitlistEntries.Count, evt.EventId, evt.Section);

        // 4. Select next user from Redis queue (FIFO) or fallback to DB order
        Guid? selectedUserId = null;

        if (redisConfig != null)
        {
            selectedUserId = await redisConfig.FifoPopAsync(evt.EventId, evt.Section);
            _logger.LogInformation("Redis FIFO selected user: {UserId}", selectedUserId);
        }

        // Fallback to DB order if Redis didn't return a user
        if (selectedUserId == null)
        {
            selectedUserId = waitlistEntries[0].UserId;
            _logger.LogInformation("Using DB order, selected user: {UserId}", selectedUserId);
        }

        if (selectedUserId == null)
        {
            _logger.LogWarning("No users available in waitlist for Event {EventId}, Section {Section}",
                evt.EventId, evt.Section);
            availableSeat.Reserved = false;
            await dbContext.SaveChangesAsync(ct);
            return;
        }

        // 5. Get the selected waitlist entry
        var selectedEntry = waitlistEntries.FirstOrDefault(w => w.UserId == selectedUserId);
        if (selectedEntry == null)
        {
            _logger.LogWarning("Selected user {UserId} not found in waitlist entries", selectedUserId);
            return;
        }

        // 6. Create OpportunityWindow
        var opportunityId = Guid.NewGuid();
        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(_waitlistSettings.OpportunityTTLMinutes);

        var opportunity = new Domain.Entities.OpportunityWindow
        {
            Id = opportunityId,
            WaitlistEntryId = selectedEntry.Id,
            SeatId = evt.SeatId,
            Token = token,
            Status = OpportunityStatus.OFFERED,
            StartsAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        // 7. Update waitlist entry status
        selectedEntry.Status = WaitlistStatus.OFFERED;
        selectedEntry.NotifiedAt = DateTime.UtcNow;

        // 8. Reserve seat for the duration of the opportunity window so it is not visible
        //    as available to other users while the selected user has the chance to purchase.
        availableSeat.Reserved = true;

        // 9. Save to database
        dbContext.OpportunityWindows.Add(opportunity);
        await dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created opportunity {OpportunityId} for user {UserId}, seat {SeatId}, expires at {ExpiresAt}",
            opportunityId, selectedUserId, evt.SeatId, expiresAt);

        // 9. Publish waitlist-opportunity event to Kafka
        if (kafkaProducer != null)
        {
            var waitlistEvent = new
            {
                OpportunityId = opportunityId,
                WaitlistEntryId = selectedEntry.Id,
                UserId = selectedUserId,
                EventId = evt.EventId,
                Section = evt.Section,
                SeatId = evt.SeatId,
                Token = token,
                ExpiresAt = expiresAt,
                Status = "OFFERED"
            };

            await kafkaProducer.ProduceAsync(_waitlistTopic, JsonSerializer.Serialize(waitlistEvent));
            _logger.LogInformation("Published waitlist-opportunity event to Kafka topic {Topic}", _waitlistTopic);
        }

        _logger.LogInformation("Successfully processed reservation expired: {ReservationId}", evt.ReservationId);
    }
}