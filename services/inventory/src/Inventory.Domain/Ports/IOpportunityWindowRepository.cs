// HUMAN CHECK: Corrección 7 - Separación de interfaces de repositorio por SRP.
using Inventory.Domain.Entities;

namespace Inventory.Domain.Ports;

public interface IOpportunityWindowRepository
{
    Task<OpportunityWindow?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OpportunityWindow?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<OpportunityWindow> AddAsync(OpportunityWindow window, CancellationToken ct = default);
    Task UpdateAsync(OpportunityWindow window, CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityWindow>> GetExpiredOpportunitiesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityWindow>> GetActiveByUserAndEventAsync(Guid userId, Guid eventId, CancellationToken ct = default);
}
