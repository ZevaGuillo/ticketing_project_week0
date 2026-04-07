using System.Text.Json.Serialization;

namespace Notification.Domain.Events;

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
