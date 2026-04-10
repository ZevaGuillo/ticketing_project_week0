using Microsoft.EntityFrameworkCore;
using Identity.Domain.Entities;
using Identity.Domain.Ports;

namespace Identity.Infrastructure.Persistence;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task SaveAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
}