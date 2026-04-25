// Application/DTOs/ProductDtos.cs
namespace ECommerce.ProductService.Application.DTOs;

// ── Requests ──
public record CreateProductRequest(
    string Name,
    string Description,
    string Brand,
    Guid CategoryId,
    List<CreateVariantRequest> Variants
);

public record CreateVariantRequest(
    string SKU,
    decimal Price,
    int StockQuantity,
    decimal? OriginalPrice = null,
    string? Size = null,
    string? Color = null
);

public record UpdateStockRequest(Guid VariantId, int Quantity);

//public record ProductSearchQuery(
//    string? SearchTerm = null,
//    string? CategoryId = null,
//    List<string>? Brands = null,
//    decimal? MinPrice = null,
//    decimal? MaxPrice = null,
//    bool InStockOnly = false,
//    string SortBy = "popularity",
//    int Page = 1,
//    int PageSize = 20
//);

// ── Responses ──
public record ProductSummaryDto(
    string Id,
    string Name,
    string Slug,
    string Brand,
    string Category,
    decimal MinPrice,
    decimal MaxPrice,
    double Rating,
    int ReviewCount,
    string? PrimaryImage,
    bool InStock
);

public record ProductDetailDto(
    string Id,
    string Name,
    string Slug,
    string Description,
    string Brand,
    string Category,
    Guid CategoryId,
    Guid SellerId,
    decimal MinPrice,
    decimal MaxPrice,
    double Rating,
    int ReviewCount,
    bool InStock,
    List<VariantDto> Variants,
    List<ProductImageDto> Images
);

public record VariantDto(
    Guid Id,
    string SKU,
    decimal Price,
    decimal? OriginalPrice,
    int StockQuantity,
    string? Size,
    string? Color,
    bool IsActive
);

public record ProductImageDto(Guid Id, string Url, bool IsPrimary, int SortOrder);

public record SearchResultDto(
    List<ProductSummaryDto> Items,
    int Total,
    int Page,
    int PageSize,
    List<FacetItem> Brands,
    decimal PriceMin,
    decimal PriceMax
);

public record FacetItem(string Value, long Count);

public record UpdateProductRequest(
    string Name,
    string Description,
    string Brand
);

public record AddProductImageRequest(
    string Url,
    bool IsPrimary = false,
    int SortOrder = 0
);