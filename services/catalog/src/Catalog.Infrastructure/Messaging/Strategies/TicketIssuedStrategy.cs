using System.Text.Json;
using Catalog.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging.Strategies;

public class TicketIssuedStrategy : IKafkaEventStrategy
{
    private readonly ILogger<TicketIssuedStrategy> _logger;

    public TicketIssuedStrategy(ILogger<TicketIssuedStrategy> logger)
        => _logger = logger;

    public string Topic => "ticket-issued";

    public async Task HandleAsync(JsonElement root, ICatalogRepository repository)
    {
        if (!root.TryGetProperty("seatId", out var sp) || !Guid.TryParse(sp.GetString(), out var seatId))
            return;

        var seat = await repository.GetSeatAsync(seatId);
        if (seat == null)
        {
            _logger.LogWarning("ticket-issued: Seat {SeatId} not found in catalog, skipping", seatId);
            return;
        }

        if (seat.IsSold())
        {
            _logger.LogInformation("ticket-issued: Seat {SeatId} already sold, skipping", seatId);
            return;
        }

        seat.Sell();
        await repository.SaveChangesAsync();
        _logger.LogInformation("ticket-issued: Seat {SeatId} marked as sold", seatId);
    }
}
