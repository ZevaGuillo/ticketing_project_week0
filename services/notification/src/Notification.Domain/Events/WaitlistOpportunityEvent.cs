namespace Notification.Domain.Events;

public class WaitlistOpportunityEvent
{
    public Guid OpportunityId { get; set; }
    public Guid WaitlistEntryId { get; set; }
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public Guid SeatId { get; set; }
    public string Section { get; set; } = string.Empty;
    public int OpportunityTTL { get; set; } = 600;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "OFFERED";
}
