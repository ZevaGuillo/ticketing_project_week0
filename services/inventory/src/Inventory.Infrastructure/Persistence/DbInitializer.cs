using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Ports;

namespace Inventory.Infrastructure.Persistence;

public class DbInitializer : IDbInitializer
{
    private readonly InventoryDbContext _db;

    public DbInitializer(InventoryDbContext db) => _db = db;

    public async Task InitializeAsync()
    {
        await _db.Database.MigrateAsync();
    }
}
