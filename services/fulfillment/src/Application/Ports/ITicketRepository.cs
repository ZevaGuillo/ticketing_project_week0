using Fulfillment.Domain.Entities;

namespace Fulfillment.Application.Ports;

public interface ITicketRepository
{
    Task<Ticket?> GetByOrderIdAsync(Guid orderId);
    Task<Ticket> CreateAsync(Ticket ticket);
    Task<Ticket> UpdateAsync(Ticket ticket);
    Task SaveChangesAsync();
}
