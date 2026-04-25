// Domain/Entities/ProductImage.cs
namespace ECommerce.ProductService.Domain.Entities;

public class ProductImage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; } = false;
    public int SortOrder { get; private set; } = 0;

    public Product Product { get; private set; } = null!;

    private ProductImage() { }

    public static ProductImage Create(Guid productId, string url,
        bool isPrimary = false, int sortOrder = 0)
        => new()
        {
            ProductId = productId,
            Url = url,
            IsPrimary = isPrimary,
            SortOrder = sortOrder
        };
}