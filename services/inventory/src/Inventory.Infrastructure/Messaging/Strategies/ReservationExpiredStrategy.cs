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

public class ReservationExpiredStrategy : IInventoryEventStrategy
{
    public string Topic => "reservation-expired";

    public async Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReservationExpiredStrategy>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var redisConfig = scope.ServiceProvider.GetService<WaitlistRedisConfiguration>();
        var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();
        var waitlistSettings = scope.ServiceProvider.GetRequiredService<WaitlistSettings>();

        var reservationId = (root.TryGetProperty("ReservationId", out var rProp) || root.TryGetProperty("reservationId", out rProp)) 
            ? rProp.GetString() : null;
        var seatIdStr = (root.TryGetProperty("SeatId", out var sProp) || root.TryGetProperty("seatId", out sProp))
            ? sProp.GetString() : null;
        var eventIdStr = (root.TryGetProperty("EventId", out var eProp) || root.TryGetProperty("eventId", out eProp))
            ? eProp.GetString() : null;
        var section = (root.TryGetProperty("Section", out var secProp) || root.TryGetProperty("section", out secProp))
            ? secProp.GetString() : null;

        if (!Guid.TryParse(seatIdStr, out var seatId) || !Guid.TryParse(eventIdStr, out var eventId) || string.IsNullOrEmpty(section))
        {
            logger.LogWarning("reservation-expired event missing required fields, skipping");
            return;
        }

        logger.LogInformation("Processing reservation-expired: {ReservationId}, Seat: {SeatId}, Event: {EventId}, Section: {Section}",
            reservationId, seatId, eventId, section);

        var availableSeat = await dbContext.Seats.FirstOrDefaultAsync(s => s.Id == seatId, ct);
        if (availableSeat == null)
        {
            logger.LogWarning("Seat {SeatId} not found in inventory, skipping waitlist processing", seatId);
            return;
        }

        logger.LogInformation("Processing seat {SeatId}: Reserved={Reserved}, Event={EventId}, Section={Section}", 
            seatId, availableSeat.Reserved, eventId, section);

        if (availableSeat.Reserved)
        {
            logger.LogWarning("Seat {SeatId} is still reserved, this shouldn't happen after expiry. Setting Reserved=false", seatId);
            availableSeat.Reserved = false;
        }

        var waitlistEntries = await dbContext.WaitlistEntries
            .Where(w => w.EventId == eventId && w.Section == section && w.Status == WaitlistStatus.ACTIVE)
            .OrderBy(w => w.JoinedAt)
            .ToListAsync(ct);

        if (!waitlistEntries.Any())
        {
            logger.LogInformation("No active waitlist entries for Event {EventId}, Section {Section}, keeping seat available", eventId, section);
            await dbContext.SaveChangesAsync(ct);
            return;
        }

        logger.LogInformation("Found {Count} waitlist entries for Event {EventId}, Section {Section}",
            waitlistEntries.Count, eventId, section);

        Guid? selectedUserId = null;
        if (redisConfig != null)
        {
            selectedUserId = await redisConfig.FifoPopAsync(eventId, section);
            logger.LogInformation("Redis FIFO selected user: {UserId}", selectedUserId);
        }

        if (selectedUserId == null)
        {
            selectedUserId = waitlistEntries[0].UserId;
            logger.LogInformation("Using DB order, selected user: {UserId}", selectedUserId);
        }

        var selectedEntry = waitlistEntries.FirstOrDefault(w => w.UserId == selectedUserId);
        if (selectedEntry == null)
        {
            logger.LogWarning("Selected user {UserId} not found in waitlist entries", selectedUserId);
            return;
        }

        var opportunityId = Guid.NewGuid();
        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(waitlistSettings.OpportunityTTLMinutes);

        var opportunity = new OpportunityWindow
        {
            Id = opportunityId,
            WaitlistEntryId = selectedEntry.Id,
            SeatId = seatId,
            Token = token,
            Status = OpportunityStatus.OFFERED,
            StartsAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        selectedEntry.Status = WaitlistStatus.OFFERED;
        selectedEntry.NotifiedAt = DateTime.UtcNow;
        availableSeat.Reserved = true;

        dbContext.OpportunityWindows.Add(opportunity);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Created opportunity {OpportunityId} for user {UserId}, seat {SeatId}, expires at {ExpiresAt}",
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
            logger.LogInformation("Published waitlist-opportunity event for user {UserId}, seat {SeatId} will be marked as offered", selectedUserId, seatId);
        }

        logger.LogInformation("Successfully processed reservation-expired: {ReservationId}", reservationId);
    }
}
