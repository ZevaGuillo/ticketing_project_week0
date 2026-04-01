using System.Text.Json.Serialization;

namespace Inventory.Domain.Events;

public class WaitlistOpportunityGrantedEvent
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

    [JsonPropertyName("idempotencyKey")]
    public string IdempotencyKey { get; set; } = string.Empty;
}
