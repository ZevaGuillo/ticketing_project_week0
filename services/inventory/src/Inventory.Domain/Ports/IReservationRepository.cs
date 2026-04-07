using Inventory.Domain.Entities;

namespace Inventory.Domain.Ports;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Reservation?> GetBySeatIdAsync(Guid seatId, CancellationToken ct = default);
    Task<Reservation> AddAsync(Reservation reservation, CancellationToken ct = default);
    Task UpdateAsync(Reservation reservation, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetExpiredReservationsAsync(CancellationToken ct = default);
}
