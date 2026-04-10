// HUMAN CHECK: Corrección 7 - Separación de interfaces de repositorio por SRP.
using Inventory.Domain.Entities;

namespace Inventory.Domain.Ports;

public interface IWaitlistRepository
{
    Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WaitlistEntry?> GetByUserEventSectionAsync(Guid userId, Guid eventId, string section, CancellationToken ct = default);
    Task<IReadOnlyList<WaitlistEntry>> GetActiveByEventAndSectionAsync(Guid eventId, string section, CancellationToken ct = default);
    Task<WaitlistEntry> AddAsync(WaitlistEntry entry, CancellationToken ct = default);
    Task UpdateAsync(WaitlistEntry entry, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, Guid eventId, string section, CancellationToken ct = default);
    Task<int> GetQueuePositionAsync(Guid eventId, string section, Guid userId, CancellationToken ct = default);
}
