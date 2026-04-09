using System.Text.Json;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Strategies;

/// <summary>
/// Handles reservation-expired events. Waitlist processing is now driven by seat-released
/// via <see cref="SeatReleasedInventoryStrategy"/>. This handler only performs a safety check
/// to ensure the seat is marked as available if somehow still reserved.
/// </summary>
public class ReservationExpiredStrategy : IInventoryEventStrategy
{
    public string Topic => "reservation-expired";

    public async Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReservationExpiredStrategy>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var seatIdStr = (root.TryGetProperty("SeatId", out var sProp) || root.TryGetProperty("seatId", out sProp))
            ? sProp.GetString() : null;
        var reservationId = (root.TryGetProperty("ReservationId", out var rProp) || root.TryGetProperty("reservationId", out rProp))
            ? rProp.GetString() : null;

        logger.LogInformation("reservation-expired received for reservation {ReservationId} — waitlist handled by seat-released", reservationId);

        // Safety guard: ensure seat is not left in reserved state if the worker
        // failed to clear it before publishing.
        if (Guid.TryParse(seatIdStr, out var seatId))
        {
            var seat = await dbContext.Seats.FirstOrDefaultAsync(s => s.Id == seatId, ct);
            if (seat is { Reserved: true })
            {
                logger.LogWarning("Seat {SeatId} was still reserved on reservation-expired — forcing Reserved=false", seatId);
                seat.Reserved = false;
                await dbContext.SaveChangesAsync(ct);
            }
        }
    }
}
