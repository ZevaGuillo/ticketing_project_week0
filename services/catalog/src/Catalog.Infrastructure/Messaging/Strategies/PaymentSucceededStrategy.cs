using System.Text.Json;
using Catalog.Application.Ports;

namespace Catalog.Infrastructure.Messaging.Strategies;

public class PaymentSucceededStrategy : IKafkaEventStrategy
{
    public string Topic => "payment-succeeded";

    public async Task HandleAsync(JsonElement root, ICatalogRepository repository)
    {
        if (root.TryGetProperty("reservationId", out var rp) && Guid.TryParse(rp.GetString(), out var resId))
            await repository.UpdateSeatStatusByReservationAsync(resId, "sold");
    }
}
