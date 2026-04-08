using System.Text.Json;
using System.Text.Json.Serialization;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Strategies;

public class PaymentSucceededStrategy : IInventoryEventStrategy
{
    public string Topic => "payment-succeeded";

    public async Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentSucceededStrategy>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var reservationIdStr = root.TryGetProperty("reservationId", out var prop) ? prop.GetString() : null;

        if (string.IsNullOrEmpty(reservationIdStr))
        {
            logger.LogWarning("payment-succeeded event missing reservationId, skipping");
            return;
        }

        if (!Guid.TryParse(reservationIdStr, out var reservationId))
        {
            logger.LogWarning("Invalid reservationId format: {ReservationId}", reservationIdStr);
            return;
        }

        var reservation = await dbContext.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

        if (reservation == null)
        {
            logger.LogWarning("Reservation {ReservationId} not found in inventory db", reservationId);
            return;
        }

        reservation.Status = "confirmed";
        logger.LogInformation("Reservation {ReservationId} confirmed for Seat {SeatId}", reservation.Id, reservation.SeatId);

        var seat = await dbContext.Seats.FindAsync(new object[] { reservation.SeatId }, ct);
        if (seat != null)
        {
            seat.Reserved = true;
            logger.LogInformation("Seat {SeatId} confirmed as RESERVED (Sold)", seat.Id);
        }

        // Mark any active OpportunityWindow as USED so the expiry worker does not re-release the seat.
        var activeWindow = await dbContext.OpportunityWindows
            .Where(o => o.SeatId == reservation.SeatId
                     && (o.Status == OpportunityStatus.OFFERED || o.Status == OpportunityStatus.IN_PROGRESS))
            .FirstOrDefaultAsync(ct);

        if (activeWindow != null)
        {
            activeWindow.Status = OpportunityStatus.USED;
            activeWindow.UsedAt = DateTime.UtcNow;
            logger.LogInformation("OpportunityWindow {OpportunityId} marked as USED after payment", activeWindow.Id);
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
