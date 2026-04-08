using System.Text.Json.Serialization;

namespace Payment.Domain.Events;

/// <summary>
/// Event published when a seat reservation is successfully created.
/// Matches the schema defined in contracts/kafka/reservation-created.json
/// </summary>
public record ReservationCreatedEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("reservationId")] string ReservationId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("seatId")] string SeatId,
    [property: JsonPropertyName("seatNumber")] string SeatNumber,
    [property: JsonPropertyName("section")] string Section,
    [property: JsonPropertyName("basePrice")] decimal BasePrice,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt,
    [property: JsonPropertyName("expiresAt")] DateTime ExpiresAt,
    [property: JsonPropertyName("status")] string Status
);
