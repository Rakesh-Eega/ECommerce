// Infrastructure/Persistence/PaymentDbContext.cs
using ECommerce.PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options) { }

    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<PaymentTransaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderId);
            e.HasIndex(x => x.PaymentIntentId);
            e.HasIndex(x => x.IdempotencyKey).IsUnique();
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.OrderId).HasMaxLength(100);
            e.Property(x => x.IdempotencyKey).HasMaxLength(200);
        });
    }
}