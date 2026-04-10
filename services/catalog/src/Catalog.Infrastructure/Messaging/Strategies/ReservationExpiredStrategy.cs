using System.Text.Json;
using Catalog.Application.Ports;

namespace Catalog.Infrastructure.Messaging.Strategies;

public class ReservationExpiredStrategy : IKafkaEventStrategy
{
    public string Topic => "reservation-expired";

    public async Task HandleAsync(JsonElement root, ICatalogRepository repository)
    {
        if (root.TryGetProperty("seatId", out var sp) && Guid.TryParse(sp.GetString(), out var seatId))
            await repository.UpdateSeatStatusAsync(seatId, "available");
        else if (root.TryGetProperty("reservationId", out var rp) && Guid.TryParse(rp.GetString(), out var resId))
            await repository.UpdateSeatStatusByReservationAsync(resId, "available");
    }
}
