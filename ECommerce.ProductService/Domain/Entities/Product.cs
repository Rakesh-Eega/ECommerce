// Domain/Entities/Product.cs
namespace ECommerce.ProductService.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public Guid SellerId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsApproved { get; private set; } = false; // Admin approves
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public Category Category { get; private set; } = null!;
    public List<ProductVariant> Variants { get; private set; } = new();
    public List<ProductImage> Images { get; private set; } = new();

    // Computed from variants
    public decimal MinPrice => Variants.Any() ? Variants.Min(v => v.Price) : 0;
    public decimal MaxPrice => Variants.Any() ? Variants.Max(v => v.Price) : 0;
    public int TotalStock => Variants.Sum(v => v.StockQuantity);

    private Product() { }

    public static Product Create(string name, string slug, string description,
        string brand, Guid categoryId, Guid sellerId)
        => new()
        {
            Name = name,
            Slug = slug.ToLowerInvariant(),
            Description = description,
            Brand = brand,
            CategoryId = categoryId,
            SellerId = sellerId
        };

    public void Update(string name, string description, string brand)
    {
        Name = name;
        Description = description;
        Brand = brand;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve() { IsApproved = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
}