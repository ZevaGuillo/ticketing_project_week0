using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Seat> Seats { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;
    public DbSet<WaitlistEntry> WaitlistEntries { get; set; } = null!;
    public DbSet<OpportunityWindow> OpportunityWindows { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_inventory");

        // HUMAN CHECK: Uso de RowVersion para Concurrencia Optimista.
        // Se asegura que si dos procesos intentan actualizar el mismo registro 
        // simultáneamente fuera del lock de Redis, el motor de DB detecte el conflicto.
        modelBuilder.Entity<Seat>(eb =>
        {
            eb.HasKey(e => e.Id);
            eb.Property(e => e.Version).IsRowVersion();
        });

        modelBuilder.Entity<Reservation>(eb =>
        {
            eb.HasKey(e => e.Id);
            eb.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            eb.Property(e => e.Status).HasMaxLength(20);
            eb.Property(e => e.CustomerId).HasMaxLength(256);
            eb.HasIndex(e => e.SeatId);
            eb.HasIndex(e => e.ExpiresAt);
        });

        modelBuilder.Entity<WaitlistEntry>(eb =>
        {
            eb.HasKey(e => e.Id);
            eb.Property(e => e.Section).HasMaxLength(100);
            eb.Property(e => e.Status).HasConversion<string>();
            eb.HasIndex(e => new { e.UserId, e.EventId, e.Section }).IsUnique();
            eb.HasIndex(e => new { e.EventId, e.Section, e.Status });
            eb.HasIndex(e => new { e.UserId, e.Status });
        });

        modelBuilder.Entity<OpportunityWindow>(eb =>
        {
            eb.HasKey(e => e.Id);
            eb.Property(e => e.Token).HasMaxLength(255);
            eb.Property(e => e.Status).HasConversion<string>();
            eb.Property(e => e.SeatId).IsRequired();
            eb.HasIndex(e => e.WaitlistEntryId);
            eb.HasIndex(e => e.Token);
            eb.HasOne(e => e.WaitlistEntry)
                .WithMany(w => w.OpportunityWindows)
                .HasForeignKey(e => e.WaitlistEntryId);
        });
    }
}
