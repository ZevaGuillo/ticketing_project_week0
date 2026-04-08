using System.Text.Json;
using Catalog.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging.Strategies;

public class WaitlistOpportunityStrategy : IKafkaEventStrategy
{
    private readonly ILogger<WaitlistOpportunityStrategy> _logger;

    public WaitlistOpportunityStrategy(ILogger<WaitlistOpportunityStrategy> logger)
        => _logger = logger;

    public string Topic => "waitlist-opportunity";

    public async Task HandleAsync(JsonElement root, ICatalogRepository repository)
    {
        JsonElement? seatIdElement = null;
        if (root.TryGetProperty("SeatId", out var pascal)) seatIdElement = pascal;
        else if (root.TryGetProperty("seatId", out var camel)) seatIdElement = camel;

        if (!seatIdElement.HasValue || !Guid.TryParse(seatIdElement.Value.GetString(), out var seatId))
            return;

        var status = (root.TryGetProperty("Status", out var sp) || root.TryGetProperty("status", out sp))
            ? sp.GetString() : "OFFERED";

        if (status == "OFFERED")
        {
            await repository.UpdateSeatStatusAsync(seatId, "reserved", null);
            _logger.LogInformation("Seat {SeatId} marked as reserved (opportunity offered)", seatId);
        }
        else if (status == "EXPIRED" || status == "USED")
        {
            await repository.UpdateSeatStatusAsync(seatId, "available", null);
            _logger.LogInformation("Seat {SeatId} marked as available (opportunity {Status})", seatId, status);
        }
    }
}
