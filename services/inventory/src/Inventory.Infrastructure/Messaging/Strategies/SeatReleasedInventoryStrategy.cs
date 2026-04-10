using System.Text.Json;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Configuration;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Strategies;

/// <summary>
/// Handles seat-released events and triggers waitlist FIFO selection.
/// This is the single entry point for waitlist processing regardless of why the seat
/// was released (TTL expiry, payment failure, manual cancellation).
/// </summary>
public class SeatReleasedInventoryStrategy : IInventoryEventStrategy
{
    public string Topic => "seat-released";

    public async Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeatReleasedInventoryStrategy>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var redisConfig = scope.ServiceProvider.GetService<WaitlistRedisConfiguration>();
        var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();
        var waitlistSettings = scope.ServiceProvider.GetRequiredService<WaitlistSettings>();

        var seatIdStr = (root.TryGetProperty("SeatId", out var sProp) || root.TryGetProperty("seatId", out sProp))
            ? sProp.GetString() : null;
        var eventIdStr = (root.TryGetProperty("EventId", out var eProp) || root.TryGetProperty("eventId", out eProp))
            ? eProp.GetString() : null;
        var section = (root.TryGetProperty("Section", out var secProp) || root.TryGetProperty("section", out secProp))
            ? secProp.GetString() : null;

        if (!Guid.TryParse(seatIdStr, out var seatId) || !Guid.TryParse(eventIdStr, out var eventId) || string.IsNullOrEmpty(section))
        {
            logger.LogWarning("seat-released event missing required fields (seatId/eventId/section), skipping waitlist processing");
            return;
        }

        var seat = await dbContext.Seats.FirstOrDefaultAsync(s => s.Id == seatId, ct);
        if (seat == null)
        {
            logger.LogWarning("Seat {SeatId} not found in inventory, skipping waitlist processing", seatId);
            return;
        }

        var waitlistEntries = await dbContext.WaitlistEntries
            .Where(w => w.EventId == eventId && w.Section == section && w.Status == WaitlistStatus.ACTIVE)
            .OrderBy(w => w.JoinedAt)
            .ToListAsync(ct);

        if (!waitlistEntries.Any())
        {
            logger.LogInformation("No active waitlist for Event {EventId}, Section {Section} — seat remains available", eventId, section);
            return;
        }

        logger.LogInformation("seat-released: found {Count} waitlist entries for Event {EventId}, Section {Section}",
            waitlistEntries.Count, eventId, section);

        // FIFO selection: prefer Redis atomic pop, fall back to DB order
        Guid? selectedUserId = null;
        if (redisConfig != null)
        {
            selectedUserId = await redisConfig.FifoPopAsync(eventId, section);
            logger.LogInformation("Redis FIFO selected user: {UserId}", selectedUserId);
        }

        if (selectedUserId == null)
        {
            selectedUserId = waitlistEntries[0].UserId;
            logger.LogInformation("Redis unavailable — using DB order, selected user: {UserId}", selectedUserId);
        }

        var selectedEntry = waitlistEntries.FirstOrDefault(w => w.UserId == selectedUserId);
        if (selectedEntry == null)
        {
            logger.LogWarning("Selected user {UserId} not found among DB waitlist entries for Event {EventId}, Section {Section}",
                selectedUserId, eventId, section);
            return;
        }

        var opportunityId = Guid.NewGuid();
        var token = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(waitlistSettings.OpportunityTTLMinutes);

        var opportunity = new OpportunityWindow
        {
            Id = opportunityId,
            WaitlistEntryId = selectedEntry.Id,
            SeatId = seatId,
            Token = token,
            Status = OpportunityStatus.OFFERED,
            StartsAt = now,
            ExpiresAt = expiresAt
        };

        selectedEntry.Status = WaitlistStatus.OFFERED;
        selectedEntry.NotifiedAt = now;
        seat.Reserved = true;  // pre-assign seat to this user's opportunity

        dbContext.OpportunityWindows.Add(opportunity);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Created OpportunityWindow {OpportunityId} for user {UserId}, seat {SeatId}, expires {ExpiresAt}",
            opportunityId, selectedUserId, seatId, expiresAt);

        if (kafkaProducer != null)
        {
            var waitlistEvent = new
            {
                OpportunityId = opportunityId,
                WaitlistEntryId = selectedEntry.Id,
                UserId = selectedUserId,
                EventId = eventId,
                Section = section,
                SeatId = seatId,
                Token = token,
                ExpiresAt = expiresAt,
                Status = "OFFERED"
            };

            await kafkaProducer.ProduceAsync("waitlist-opportunity", JsonSerializer.Serialize(waitlistEvent));
            logger.LogInformation("Published waitlist-opportunity for user {UserId}, seat {SeatId}", selectedUserId, seatId);
        }
    }
}
