// Infrastructure/Persistence/ProductDbContext.cs
using ECommerce.ProductService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.ProductService.Infrastructure.Persistence;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Category
        builder.Entity<Category>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Parent)
             .WithMany(x => x.Children)
             .HasForeignKey(x => x.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Product
        builder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(500).IsRequired();
            e.Property(x => x.Brand).HasMaxLength(200);
            e.Ignore(x => x.MinPrice);
            e.Ignore(x => x.MaxPrice);
            e.Ignore(x => x.TotalStock);
            e.HasOne(x => x.Category)
             .WithMany(x => x.Products)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductVariant
        builder.Entity<ProductVariant>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SKU).IsUnique();
            e.Property(x => x.Price).HasColumnType("decimal(18,2)");
            e.Property(x => x.OriginalPrice).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Product)
             .WithMany(x => x.Variants)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductImage
        builder.Entity<ProductImage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Url).HasMaxLength(1000).IsRequired();
            e.HasOne(x => x.Product)
             .WithMany(x => x.Images)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        SeedCategories(builder);
    }

    private static void SeedCategories(ModelBuilder builder)
    {
        var electronics = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fashion = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var home = Guid.Parse("33333333-3333-3333-3333-333333333333");

        builder.Entity<Category>().HasData(
            new { Id = electronics, Name = "Electronics", Slug = "electronics", IsActive = true, CreatedAt = DateTime.UtcNow, ParentId = (Guid?)null, ImageUrl = (string?)null },
            new { Id = fashion, Name = "Fashion", Slug = "fashion", IsActive = true, CreatedAt = DateTime.UtcNow, ParentId = (Guid?)null, ImageUrl = (string?)null },
            new { Id = home, Name = "Home & Living", Slug = "home-living", IsActive = true, CreatedAt = DateTime.UtcNow, ParentId = (Guid?)null, ImageUrl = (string?)null }
        );
    }
}