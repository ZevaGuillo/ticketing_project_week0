using System.Text.Json;
using Catalog.Application.Ports;

namespace Catalog.Infrastructure.Messaging.Strategies;

public class SeatReleasedStrategy : IKafkaEventStrategy
{
    public string Topic => "seat-released";

    public async Task HandleAsync(JsonElement root, ICatalogRepository repository)
    {
        if (root.TryGetProperty("seatId", out var sp) && Guid.TryParse(sp.GetString(), out var seatId))
            await repository.UpdateSeatStatusAsync(seatId, "available", null);
    }
}
