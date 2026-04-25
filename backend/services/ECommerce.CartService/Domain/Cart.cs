// Domain/Cart.cs
namespace ECommerce.CartService.Domain;

public class Cart
{
    public string UserId { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = new();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public decimal Total => Items.Sum(i => i.Price * i.Quantity);
    public int ItemCount => Items.Sum(i => i.Quantity);

    public void AddItem(CartItem newItem)
    {
        var existing = Items.FirstOrDefault(i =>
            i.ProductId == newItem.ProductId &&
            i.VariantId == newItem.VariantId);

        if (existing is not null)
            existing.Quantity += newItem.Quantity;
        else
            Items.Add(newItem);

        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(string variantId)
    {
        Items.RemoveAll(i => i.VariantId == variantId);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateQuantity(string variantId, int quantity)
    {
        var item = Items.FirstOrDefault(i => i.VariantId == variantId);
        if (item is null) return;

        if (quantity <= 0)
            Items.Remove(item);
        else
            item.Quantity = quantity;

        UpdatedAt = DateTime.UtcNow;
    }

    public void Clear()
    {
        Items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }
}

public class CartItem
{
    public string ProductId { get; set; } = string.Empty;
    public string VariantId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal Price { get; set; } // Always set server-side
    public int Quantity { get; set; }
    public int MaxStock { get; set; }
}