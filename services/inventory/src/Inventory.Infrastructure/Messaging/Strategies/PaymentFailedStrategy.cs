using System.Text.Json;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Messaging.Strategies;

/// <summary>
/// Handles payment-failed events by releasing the reserved seat and publishing
/// a seat-released event, which in turn triggers waitlist processing via
/// <see cref="SeatReleasedInventoryStrategy"/>.
/// </summary>
public class PaymentFailedStrategy : IInventoryEventStrategy
{
    public string Topic => "payment-failed";

    public async Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken ct)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentFailedStrategy>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();

        var reservationIdStr = (root.TryGetProperty("reservationId", out var rProp) || root.TryGetProperty("ReservationId", out rProp))
            ? rProp.GetString() : null;

        if (!Guid.TryParse(reservationIdStr, out var reservationId))
        {
            logger.LogWarning("payment-failed event missing or invalid reservationId, skipping");
            return;
        }

        var reservation = await dbContext.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId, ct);

        if (reservation == null)
        {
            logger.LogWarning("Reservation {ReservationId} not found, skipping seat release", reservationId);
            return;
        }

        if (reservation.Status == "expired" || reservation.Status == "cancelled")
        {
            logger.LogInformation("Reservation {ReservationId} already {Status}, skipping duplicate release", reservationId, reservation.Status);
            return;
        }

        var seat = await dbContext.Seats.FindAsync(new object[] { reservation.SeatId }, ct);
        if (seat == null)
        {
            logger.LogWarning("Seat {SeatId} not found for reservation {ReservationId}", reservation.SeatId, reservationId);
            return;
        }

        reservation.Status = "cancelled";
        seat.Reserved = false;
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Released seat {SeatId} for failed payment on reservation {ReservationId}", seat.Id, reservationId);

        if (kafkaProducer != null)
        {
            var seatReleasedEvent = new
            {
                seatId = seat.Id.ToString("D"),
                eventId = reservation.EventId.ToString("D"),
                section = seat.Section,
                releasedAt = DateTime.UtcNow,
                reason = "payment_failed"
            };

            try
            {
                await kafkaProducer.ProduceAsync("seat-released", JsonSerializer.Serialize(seatReleasedEvent));
                logger.LogInformation("Published seat-released for seat {SeatId} (payment_failed)", seat.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish seat-released for seat {SeatId}", seat.Id);
            }
        }
    }
}
