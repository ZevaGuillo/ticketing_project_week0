using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Events;

namespace Ordering.Infrastructure.Events.Strategies;

public class ReservationCreatedStrategy : IOrderingEventStrategy
{
    public string Topic => "reservation-created";

    public Task HandleAsync(JsonElement root, IServiceScope scope, CancellationToken cancellationToken)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReservationCreatedStrategy>>();
        var reservationStore = scope.ServiceProvider.GetRequiredService<ReservationStore>();

        var evt = root.Deserialize<ReservationCreatedEvent>(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (evt != null)
        {
            reservationStore.AddReservation(evt);
            logger.LogInformation("Processed reservation-created event for reservation {ReservationId}",
                evt.ReservationId);
        }

        return Task.CompletedTask;
    }
}
