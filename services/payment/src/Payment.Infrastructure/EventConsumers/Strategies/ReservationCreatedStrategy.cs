using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Payment.Domain.Events;
using Payment.Infrastructure.Services;

namespace Payment.Infrastructure.EventConsumers.Strategies;

public class ReservationCreatedStrategy : IPaymentEventStrategy
{
    public string Topic => "reservation-created";

    public Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken cancellationToken)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReservationCreatedStrategy>>();
        var reservationStore = scope.ServiceProvider.GetRequiredService<ReservationStateStore>();

        var evt = root.Deserialize<ReservationCreatedEvent>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evt == null)
        {
            logger.LogWarning("Failed to deserialize reservation-created event");
            return Task.CompletedTask;
        }

        if (!Guid.TryParse(evt.ReservationId, out var reservationId) ||
            !Guid.TryParse(evt.CustomerId, out var customerId) ||
            !Guid.TryParse(evt.SeatId, out var seatId))
        {
            logger.LogWarning("Invalid GUIDs in reservation-created event");
            return Task.CompletedTask;
        }

        reservationStore.AddReservation(reservationId, customerId, seatId, evt.ExpiresAt);
        logger.LogInformation("Processed reservation-created event for reservation {ReservationId}", reservationId);
        return Task.CompletedTask;
    }
}
