using Microsoft.EntityFrameworkCore;

namespace Payment.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options) { }

    // DbSets will be added in subsequent tasks when entities are created

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_payment");

        // Entity configurations will be added when entities are created
    }
}