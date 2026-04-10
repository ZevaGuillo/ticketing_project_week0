using Inventory.Domain.Entities;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class ReservationRepository : IReservationRepository
{
    private readonly InventoryDbContext _context;

    public ReservationRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<Reservation?> GetBySeatIdAsync(Guid seatId, CancellationToken ct = default)
    {
        return await _context.Reservations
            .Where(r => r.SeatId == seatId && r.Status == "active")
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Reservation> AddAsync(Reservation reservation, CancellationToken ct = default)
    {
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync(ct);
        return reservation;
    }

    public async Task UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Reservation>> GetExpiredReservationsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Reservations
            .Where(r => r.Status == "active" && r.ExpiresAt <= now)
            .ToListAsync(ct);
    }
}