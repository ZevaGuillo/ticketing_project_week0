using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Seat> Seats { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_inventory");

        modelBuilder.Entity<Seat>(eb =>
        {
            eb.HasKey(e => e.Id);
            eb.Property(e => e.Version).IsRowVersion();
        });
    }
}
