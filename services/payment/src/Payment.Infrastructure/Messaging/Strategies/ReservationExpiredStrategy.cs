using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Payment.Domain.Events;
using Payment.Infrastructure.Services;

namespace Payment.Infrastructure.Messaging.Strategies;

public class ReservationExpiredStrategy : IPaymentEventStrategy
{
    public string Topic => "reservation-expired";

    public Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken cancellationToken)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReservationExpiredStrategy>>();
        var reservationStore = scope.ServiceProvider.GetRequiredService<ReservationStateStore>();

        var evt = root.Deserialize<ReservationExpiredEvent>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (evt == null)
        {
            logger.LogWarning("Failed to deserialize reservation-expired event");
            return Task.CompletedTask;
        }

        if (!Guid.TryParse(evt.ReservationId, out var reservationId))
        {
            logger.LogWarning("Invalid ReservationId GUID in reservation-expired event");
            return Task.CompletedTask;
        }

        reservationStore.ExpireReservation(reservationId);
        logger.LogInformation("Processed reservation-expired event for reservation {ReservationId}", reservationId);
        return Task.CompletedTask;
    }
}
