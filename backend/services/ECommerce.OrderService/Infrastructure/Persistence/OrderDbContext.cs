// Infrastructure/Persistence/OrderDbContext.cs
using ECommerce.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ShippingAddress> ShippingAddresses => Set<ShippingAddress>();
    public DbSet<OrderStatusHistory> StatusHistories => Set<OrderStatusHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Order
        builder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.HasIndex(x => x.CustomerId);
            e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.DeliveryCharge).HasColumnType("decimal(18,2)");
            e.Property(x => x.Discount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.PaymentStatus).HasConversion<int>();
        });

        // OrderItem
        builder.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Ignore(x => x.SubTotal);
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Order)
             .WithMany(x => x.Items)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ShippingAddress — owned entity per order
        builder.Entity<ShippingAddress>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Order)
             .WithOne(x => x.ShippingAddress)
             .HasForeignKey<ShippingAddress>(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderStatusHistory
        builder.Entity<OrderStatusHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<int>();
            e.HasOne(x => x.Order)
             .WithMany(x => x.StatusHistory)
             .HasForeignKey(x => x.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}