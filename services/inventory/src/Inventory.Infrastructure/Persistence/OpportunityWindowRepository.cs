using Inventory.Domain.Entities;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class OpportunityWindowRepository : IOpportunityWindowRepository
{
    private readonly InventoryDbContext _context;

    public OpportunityWindowRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<OpportunityWindow?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.OpportunityWindows
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<OpportunityWindow?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _context.OpportunityWindows
            .FirstOrDefaultAsync(e => e.Token == token, ct);
    }

    public async Task<OpportunityWindow> AddAsync(OpportunityWindow window, CancellationToken ct = default)
    {
        _context.OpportunityWindows.Add(window);
        await _context.SaveChangesAsync(ct);
        return window;
    }

    public async Task UpdateAsync(OpportunityWindow window, CancellationToken ct = default)
    {
        _context.OpportunityWindows.Update(window);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OpportunityWindow>> GetExpiredOpportunitiesAsync(CancellationToken ct = default)
    {
        return await _context.OpportunityWindows
            .Where(e => e.Status == Domain.Enums.OpportunityStatus.OFFERED && e.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(ct);
    }
}