using System.Text.Json.Serialization;

namespace Notification.Infrastructure.Events;

public class TicketIssuedEvent
{
    [JsonPropertyName("ticket_id")]
    public Guid TicketId { get; set; }

    [JsonPropertyName("order_id")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("customer_email")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("event_name")]
    public string EventName { get; set; } = string.Empty;

    [JsonPropertyName("seat_number")]
    public string SeatNumber { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";

    [JsonPropertyName("ticket_pdf_url")]
    public string? TicketPdfUrl { get; set; }

    [JsonPropertyName("qr_code_data")]
    public string? QrCodeData { get; set; }

    [JsonPropertyName("issued_at")]
    public DateTime IssuedAt { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class WaitlistOpportunityEvent
{
    [JsonPropertyName("opportunityId")]
    public Guid OpportunityId { get; set; }

    [JsonPropertyName("waitlistEntryId")]
    public Guid WaitlistEntryId { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("seatId")]
    public Guid SeatId { get; set; }

    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    [JsonPropertyName("opportunityTTL")]
    public int OpportunityTTL { get; set; } = 600;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "OFFERED";
}
