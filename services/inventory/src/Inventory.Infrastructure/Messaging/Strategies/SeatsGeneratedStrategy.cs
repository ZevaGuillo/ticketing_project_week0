using System.Text.Json;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Strategies;

public class SeatsGeneratedStrategy : IInventoryEventStrategy
{
    public string Topic => "seats-generated";

    public async Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeatsGeneratedStrategy>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        if (!root.TryGetProperty("eventId", out var eventIdProp) ||
            !root.TryGetProperty("seats", out var seatsProp) ||
            seatsProp.ValueKind != JsonValueKind.Array)
        {
            logger.LogWarning("seats-generated event missing required fields, skipping");
            return;
        }

        var eventId = eventIdProp.GetGuid();
        var inventorySeats = new List<Seat>();

        foreach (var seatEl in seatsProp.EnumerateArray())
        {
            if (!seatEl.TryGetProperty("seatId", out var seatIdProp) || !seatIdProp.TryGetGuid(out var seatId))
                continue;

            var exists = await dbContext.Seats.FindAsync(new object[] { seatId }, ct);
            if (exists != null)
            {
                logger.LogDebug("Seat {SeatId} already exists, skipping (idempotency)", seatId);
                continue;
            }

            inventorySeats.Add(new Seat
            {
                Id = seatId,
                Section = seatEl.TryGetProperty("section", out var s) ? s.GetString()! : string.Empty,
                Row = seatEl.TryGetProperty("row", out var r) ? r.GetString()! : string.Empty,
                Number = seatEl.TryGetProperty("number", out var n) ? n.GetInt32() : 0,
                Reserved = false
            });
        }

        if (inventorySeats.Count > 0)
        {
            await dbContext.Seats.AddRangeAsync(inventorySeats, ct);
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Inserted {Count} seats for event {EventId}", inventorySeats.Count, eventId);
        }
        else
        {
            logger.LogInformation("No new seats to insert for event {EventId} (all already exist)", eventId);
        }
    }
}
