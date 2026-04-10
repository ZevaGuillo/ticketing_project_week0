using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public class WaitlistEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public string Section { get; set; } = string.Empty;
    public WaitlistStatus Status { get; set; } = WaitlistStatus.ACTIVE;
    public DateTime JoinedAt { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<OpportunityWindow> OpportunityWindows { get; set; } = new List<OpportunityWindow>();
}
