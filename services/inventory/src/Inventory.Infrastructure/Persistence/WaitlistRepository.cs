using Inventory.Domain.Entities;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class WaitlistRepository : IWaitlistRepository
{
    private readonly InventoryDbContext _context;

    public WaitlistRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.WaitlistEntries
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<WaitlistEntry?> GetByUserEventSectionAsync(Guid userId, Guid eventId, string section, CancellationToken ct = default)
    {
        return await _context.WaitlistEntries
            .FirstOrDefaultAsync(e => e.UserId == userId && e.EventId == eventId && e.Section == section, ct);
    }

    public async Task<IReadOnlyList<WaitlistEntry>> GetActiveByEventAndSectionAsync(Guid eventId, string section, CancellationToken ct = default)
    {
        return await _context.WaitlistEntries
            .Where(e => e.EventId == eventId && e.Section == section && e.Status == Domain.Enums.WaitlistStatus.ACTIVE)
            .OrderBy(e => e.JoinedAt)
            .ToListAsync(ct);
    }

    public async Task<WaitlistEntry> AddAsync(WaitlistEntry entry, CancellationToken ct = default)
    {
        _context.WaitlistEntries.Add(entry);
        await _context.SaveChangesAsync(ct);
        return entry;
    }

    public async Task UpdateAsync(WaitlistEntry entry, CancellationToken ct = default)
    {
        _context.WaitlistEntries.Update(entry);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid eventId, string section, CancellationToken ct = default)
    {
        return await _context.WaitlistEntries
            .AnyAsync(e => e.UserId == userId && e.EventId == eventId && e.Section == section
                        && (e.Status == Domain.Enums.WaitlistStatus.ACTIVE || e.Status == Domain.Enums.WaitlistStatus.OFFERED), ct);
    }

    public async Task<int> GetQueuePositionAsync(Guid eventId, string section, Guid userId, CancellationToken ct = default)
    {
        return await _context.WaitlistEntries
            .Where(e => e.EventId == eventId && e.Section == section && e.Status == Domain.Enums.WaitlistStatus.ACTIVE)
            .Where(e => e.JoinedAt <= (
                _context.WaitlistEntries
                    .Where(w => w.UserId == userId && w.EventId == eventId && w.Section == section)
                    .Select(w => w.JoinedAt)
                    .FirstOrDefault()
            ))
            .CountAsync(ct);
    }
}