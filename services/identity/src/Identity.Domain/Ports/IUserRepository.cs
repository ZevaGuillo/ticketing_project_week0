namespace Identity.Domain.Ports;
using Identity.Domain.Entities;


public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task SaveAsync(User user);
}