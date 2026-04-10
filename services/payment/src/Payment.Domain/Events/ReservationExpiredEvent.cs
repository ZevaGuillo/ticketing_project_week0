using System.Text.Json.Serialization;

namespace Payment.Domain.Events;

/// <summary>
/// Event published when a reservation expires.
/// Matches the schema defined in contracts/kafka/reservation-expired.json
/// </summary>
public record ReservationExpiredEvent(
    [property: JsonPropertyName("reservationId")] string ReservationId,
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("seatId")] string SeatId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("expiredAt")] DateTime ExpiredAt,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("status")] string Status
);
