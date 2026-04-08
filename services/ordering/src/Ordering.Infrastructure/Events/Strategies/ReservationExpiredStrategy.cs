using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Events;

namespace Ordering.Infrastructure.Events.Strategies;

public class ReservationExpiredStrategy : IOrderingEventStrategy
{
    public string Topic => "reservation-expired";

    public Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken cancellationToken)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReservationExpiredStrategy>>();
        var reservationStore = scope.ServiceProvider.GetRequiredService<ReservationStore>();

        var evt = root.Deserialize<ReservationExpiredEvent>(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (evt != null)
        {
            reservationStore.RemoveReservation(evt);
            logger.LogInformation("Processed reservation-expired event for reservation {ReservationId}",
                evt.ReservationId);
        }

        return Task.CompletedTask;
    }
}
