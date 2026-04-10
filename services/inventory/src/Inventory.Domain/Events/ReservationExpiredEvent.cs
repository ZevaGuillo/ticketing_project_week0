using System.Text.Json.Serialization;

namespace Inventory.Domain.Events;

public class ReservationExpiredEvent
{
    [JsonPropertyName("reservationId")]
    public Guid ReservationId { get; set; }

    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("seatId")]
    public Guid SeatId { get; set; }

    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    [JsonPropertyName("expiredAt")]
    public DateTime ExpiredAt { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "TTL_EXPIRED";
}
