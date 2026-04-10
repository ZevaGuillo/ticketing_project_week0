using Inventory.Domain.Entities;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Ports;

public interface IWaitlistService
{
    Task<WaitlistEntry> JoinWaitlistAsync(Guid userId, Guid eventId, string section, CancellationToken ct = default);
    Task<WaitlistEntry?> GetStatusAsync(Guid userId, Guid eventId, string section, CancellationToken ct = default);
    Task CancelWaitlistAsync(Guid entryId, CancellationToken ct = default);
    Task<int> GetQueuePositionAsync(Guid eventId, string section, Guid userId, CancellationToken ct = default);
    Task<OpportunityWindow> CreateOpportunityAsync(WaitlistEntry entry, Guid seatId, CancellationToken ct = default);
    Task<OpportunityWindow?> ValidateOpportunityAsync(string token, CancellationToken ct = default);
    Task MarkOpportunityUsedAsync(Guid opportunityId, CancellationToken ct = default);
}
