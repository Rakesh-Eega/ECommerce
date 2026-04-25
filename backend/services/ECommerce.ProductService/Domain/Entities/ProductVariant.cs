// Domain/Entities/ProductVariant.cs
namespace ECommerce.ProductService.Domain.Entities;

public class ProductVariant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public string SKU { get; private set; } = string.Empty;
    public string? Size { get; private set; }
    public string? Color { get; private set; }
    public decimal Price { get; private set; }
    public decimal? OriginalPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Product Product { get; private set; } = null!;

    private ProductVariant() { }

    public static ProductVariant Create(Guid productId, string sku,
        decimal price, int stock, decimal? originalPrice = null,
        string? size = null, string? color = null)
        => new()
        {
            ProductId = productId,
            SKU = sku.ToUpperInvariant(),
            Price = price,
            OriginalPrice = originalPrice,
            StockQuantity = stock,
            Size = size,
            Color = color
        };

    public void UpdateStock(int quantity) => StockQuantity = quantity;

    public void DeductStock(int quantity)
    {
        if (quantity > StockQuantity)
            throw new InvalidOperationException("Insufficient stock.");
        StockQuantity -= quantity;
    }

    public void UpdatePrice(decimal price, decimal? originalPrice = null)
    {
        Price = price;
        OriginalPrice = originalPrice;
    }
}