using System.Text.Json.Serialization;

namespace Payment.Domain.Events;

/// <summary>
/// Event published when a payment fails.
/// Matches the schema defined in contracts/kafka/payment-failed.json
/// </summary>
public record PaymentFailedEvent(
    [property: JsonPropertyName("paymentId")] string PaymentId,
    [property: JsonPropertyName("orderId")] string OrderId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("reservationId")] string? ReservationId,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("paymentMethod")] string PaymentMethod,
    [property: JsonPropertyName("errorCode")] string? ErrorCode,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage,
    [property: JsonPropertyName("failureReason")] string? FailureReason,
    [property: JsonPropertyName("attemptedAt")] DateTime AttemptedAt,
    [property: JsonPropertyName("status")] string Status = "failed"
);
