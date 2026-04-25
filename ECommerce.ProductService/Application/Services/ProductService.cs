// Application/Services/ProductService.cs
using ECommerce.ProductService.Application.DTOs;
using ECommerce.ProductService.Domain.Entities;
using ECommerce.ProductService.Infrastructure.Persistence;
using ECommerce.ProductService.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.ProductService.Application.Services;

public interface IProductService
{
    Task<(ProductDetailDto? Product, string? Error)> CreateAsync(
        CreateProductRequest request, Guid sellerId);
    Task<ProductDetailDto?> GetBySlugAsync(string slug);
    Task<ProductDetailDto?> GetByIdAsync(Guid id);
    Task<SearchResultDto> SearchAsync(ProductSearchQuery query);
    Task<(bool Success, string? Error)> ApproveAsync(Guid productId);
    Task<(bool Success, string? Error)> UpdateStockAsync(UpdateStockRequest request);
}

public class ProductService : IProductService
{
    private readonly ProductDbContext _db;
    private readonly IElasticsearchService _search;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ProductDbContext db,
        IElasticsearchService search,
        ILogger<ProductService> logger)
    {
        _db = db;
        _search = search;
        _logger = logger;
    }

    public async Task<(ProductDetailDto?, string?)> CreateAsync(
        CreateProductRequest request, Guid sellerId)
    {
        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.IsActive);
        if (category is null)
            return (null, "Category not found.");

        var slug = GenerateSlug(request.Name);
        var slugExists = await _db.Products.AnyAsync(p => p.Slug == slug);
        if (slugExists)
            slug = $"{slug}-{Guid.NewGuid().ToString()[..6]}";

        var product = Product.Create(
            request.Name, slug, request.Description,
            request.Brand, request.CategoryId, sellerId);

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        // Add variants
        foreach (var v in request.Variants)
        {
            var variant = ProductVariant.Create(
                product.Id, v.SKU, v.Price,
                v.StockQuantity, v.OriginalPrice, v.Size, v.Color);
            _db.ProductVariants.Add(variant);
        }

        await _db.SaveChangesAsync();

        // Reload with all relations
        var created = await GetFullProductAsync(product.Id);
        if (created is null) return (null, "Failed to load product after creation.");

        _logger.LogInformation("Product created: {ProductId} by Seller: {SellerId}",
            product.Id, sellerId);

        return (MapToDetailDto(created), null);
    }

    public async Task<ProductDetailDto?> GetBySlugAsync(string slug)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive))
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive && p.IsApproved);

        return product is null ? null : MapToDetailDto(product);
    }

    public async Task<ProductDetailDto?> GetByIdAsync(Guid id)
    {
        var product = await GetFullProductAsync(id);
        return product is null ? null : MapToDetailDto(product);
    }

    public async Task<SearchResultDto> SearchAsync(ProductSearchQuery query)
        => await _search.SearchAsync(query);

    public async Task<(bool, string?)> ApproveAsync(Guid productId)
    {
        var product = await GetFullProductAsync(productId);
        if (product is null) return (false, "Product not found.");

        product.Approve();
        await _db.SaveChangesAsync();

        // Index into Elasticsearch after approval
        await _search.IndexProductAsync(MapToDocument(product));

        _logger.LogInformation("Product approved: {ProductId}", productId);
        return (true, null);
    }

    public async Task<(bool, string?)> UpdateStockAsync(UpdateStockRequest request)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == request.VariantId);

        if (variant is null) return (false, "Variant not found.");

        variant.UpdateStock(request.Quantity);
        await _db.SaveChangesAsync();

        // Update Elasticsearch stock
        var product = await GetFullProductAsync(variant.ProductId);
        if (product is not null)
            await _search.UpdateProductAsync(MapToDocument(product));

        return (true, null);
    }

    // ── Private Helpers ──
    private async Task<Product?> GetFullProductAsync(Guid id)
        => await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);

    private static string GenerateSlug(string name)
        => name.ToLowerInvariant()
               .Replace(" ", "-")
               .Replace("&", "and")
               .Replace("'", "")
               .Replace(",", "")
               .Trim('-');

    private static ProductDetailDto MapToDetailDto(Product p) => new(
        Id: p.Id.ToString(),
        Name: p.Name,
        Slug: p.Slug,
        Description: p.Description,
        Brand: p.Brand,
        Category: p.Category.Name,
        CategoryId: p.CategoryId,
        SellerId: p.SellerId,
        MinPrice: p.Variants.Any() ? p.Variants.Min(v => v.Price) : 0,
        MaxPrice: p.Variants.Any() ? p.Variants.Max(v => v.Price) : 0,
        Rating: 0,
        ReviewCount: 0,
        InStock: p.Variants.Any(v => v.StockQuantity > 0),
        Variants: p.Variants.Select(v => new VariantDto(
            v.Id, v.SKU, v.Price, v.OriginalPrice,
            v.StockQuantity, v.Size, v.Color, v.IsActive)).ToList(),
        Images: p.Images.Select(i => new ProductImageDto(
            i.Id, i.Url, i.IsPrimary, i.SortOrder)).ToList()
    );

    private static ProductDocument MapToDocument(Product p) => new()
    {
        Id = p.Id.ToString(),
        Name = p.Name,
        Slug = p.Slug,
        Description = p.Description,
        Brand = p.Brand,
        Category = p.Category.Name,
        CategoryId = p.CategoryId.ToString(),
        MinPrice = (decimal)(p.Variants.Any() ? (double)p.Variants.Min(v => v.Price) : 0),
        MaxPrice = (decimal)(p.Variants.Any() ? (double)p.Variants.Max(v => v.Price) : 0),
        TotalStock = p.Variants.Sum(v => v.StockQuantity),
        Rating = 0,
        ReviewCount = 0,
        IsActive = p.IsActive,
        IsApproved = p.IsApproved,
        PrimaryImage = p.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
        CreatedAt = p.CreatedAt
    };
}