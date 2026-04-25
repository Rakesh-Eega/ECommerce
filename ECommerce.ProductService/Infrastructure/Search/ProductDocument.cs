// Infrastructure/Search/ProductDocument.cs
namespace ECommerce.ProductService.Infrastructure.Search;

public class ProductDocument
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int TotalStock { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public string? PrimaryImage { get; set; }
    public DateTime CreatedAt { get; set; }
}