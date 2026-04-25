// Domain/Entities/OrderItem.cs
namespace ECommerce.OrderService.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public string ProductId { get; private set; } = string.Empty;
    public string VariantId { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public string SKU { get; private set; } = string.Empty;
    public string? Size { get; private set; }
    public string? Color { get; private set; }
    public string? ImageUrl { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal SubTotal => UnitPrice * Quantity;

    public Order Order { get; private set; } = null!;

    private OrderItem() { }

    public static OrderItem Create(Guid orderId, string productId,
        string variantId, string productName, string sku,
        decimal unitPrice, int quantity,
        string? size = null, string? color = null,
        string? imageUrl = null)
        => new()
        {
            OrderId = orderId,
            ProductId = productId,
            VariantId = variantId,
            ProductName = productName,
            SKU = sku,
            UnitPrice = unitPrice,
            Quantity = quantity,
            Size = size,
            Color = color,
            ImageUrl = imageUrl
        };
}