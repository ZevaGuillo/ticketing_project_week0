using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public class OpportunityWindow
{
    public Guid Id { get; set; }
    public Guid WaitlistEntryId { get; set; }
    public Guid SeatId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public OpportunityStatus Status { get; set; } = OpportunityStatus.OFFERED;
    public DateTime? UsedAt { get; set; }

    public WaitlistEntry? WaitlistEntry { get; set; }
}
