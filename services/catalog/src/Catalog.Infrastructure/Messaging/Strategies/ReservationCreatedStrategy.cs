using System.Text.Json;
using Catalog.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Catalog.Infrastructure.Messaging.Strategies;

public class ReservationCreatedStrategy : IKafkaEventStrategy
{
    private readonly ILogger<ReservationCreatedStrategy> _logger;

    public ReservationCreatedStrategy(ILogger<ReservationCreatedStrategy> logger)
        => _logger = logger;

    public string Topic => "reservation-created";

    public async Task HandleAsync(JsonElement root, ICatalogRepository repository)
    {
        if (!root.TryGetProperty("seatId", out var sp) || !Guid.TryParse(sp.GetString(), out var seatId))
            return;

        Guid? reservationId = null;
        if (root.TryGetProperty("reservationId", out var rp) && Guid.TryParse(rp.GetString(), out var resId))
            reservationId = resId;

        await repository.UpdateSeatStatusAsync(seatId, "reserved", reservationId);
        _logger.LogInformation("Seat {SeatId} marked as reserved", seatId);
    }
}
