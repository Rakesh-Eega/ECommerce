// Controllers/ProductsController.cs
using ECommerce.ProductService.Application.DTOs;
using ECommerce.ProductService.Application.Services;
using ECommerce.ProductService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerce.ProductService.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ProductDbContext _db;

    public ProductsController(IProductService productService,
                               ProductDbContext db)
    {
        _productService = productService;
        _db = db;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim missing."));

    // ════════════════════════════════════════════════════════
    // PUBLIC ENDPOINTS
    // ════════════════════════════════════════════════════════

    /// GET /api/products/search?searchTerm=sony&categoryId=xxx&minPrice=100
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] ProductSearchQuery query)
        => Ok(await _productService.SearchAsync(query));

    /// GET /api/products/categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.ImageUrl,
                c.ParentId
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// GET /api/products/slug/{slug}  ← FIXED: explicit prefix avoids conflict
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var product = await _productService.GetBySlugAsync(slug);
        return product is null
            ? NotFound(new { message = "Product not found." })
            : Ok(product);
    }

    /// GET /api/products/id/{productId}
    [HttpGet("id/{productId:guid}")]
    public async Task<IActionResult> GetById(Guid productId)
    {
        var product = await _productService.GetByIdAsync(productId);
        return product is null
            ? NotFound(new { message = "Product not found." })
            : Ok(product);
    }

    // ════════════════════════════════════════════════════════
    // INTERNAL — Used by CartService via HTTP
    // ════════════════════════════════════════════════════════

    /// GET /api/products/variants/{variantId}
    [HttpGet("variants/{variantId:guid}")]
    public async Task<IActionResult> GetVariantInfo(Guid variantId)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.IsActive);

        if (variant is null)
            return NotFound(new { message = "Variant not found." });

        return Ok(new
        {
            ProductName = variant.Product.Name,
            Price = variant.Price,
            StockQuantity = variant.StockQuantity,
            ImageUrl = variant.Product.Images
                                .FirstOrDefault(i => i.IsPrimary)?.Url,
            Size = variant.Size,
            Color = variant.Color
        });
    }

    // ════════════════════════════════════════════════════════
    // SELLER ENDPOINTS
    // ════════════════════════════════════════════════════════

    /// POST /api/products
    [HttpPost]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request)
    {
        var (product, error) = await _productService
            .CreateAsync(request, CurrentUserId);

        if (error is not null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetBySlug),
            new { slug = product!.Slug }, product);
    }

    /// POST /api/products/admin/reindex
    [HttpPost("admin/reindex")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReindexAll()
    {
        var count = await _productService.ReIndexAllProductsAsync();
        return Ok(new { message = $"Reindexed {count} products successfully." });
    }

    /// PUT /api/products/{productId}
    [HttpPut("{productId:guid}")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<IActionResult> Update(
        Guid productId,
        [FromBody] UpdateProductRequest request)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return NotFound(new { message = "Product not found." });

        // Seller can only update their own products
        if (product.SellerId != CurrentUserId &&
            !User.IsInRole("Admin"))
            return Forbid();

        product.Update(request.Name, request.Description, request.Brand);
        await _db.SaveChangesAsync();

        return Ok(await _productService.GetByIdAsync(productId));
    }

    /// DELETE /api/products/{productId}
    [HttpDelete("{productId:guid}")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<IActionResult> Deactivate(Guid productId)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return NotFound(new { message = "Product not found." });

        if (product.SellerId != CurrentUserId &&
            !User.IsInRole("Admin"))
            return Forbid();

        product.Deactivate();
        await _db.SaveChangesAsync();

        return Ok(new { message = "Product deactivated." });
    }

    /// GET /api/products/seller/my-products
    [HttpGet("seller/my-products")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<IActionResult> GetMyProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Where(p => p.SellerId == CurrentUserId);

        var total = await query.CountAsync();
        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items = products });
    }

    /// PATCH /api/products/variants/stock
    [HttpPatch("variants/stock")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<IActionResult> UpdateStock(
        [FromBody] UpdateStockRequest request)
    {
        var (success, error) = await _productService
            .UpdateStockAsync(request);

        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(new { message = "Stock updated." });
    }

    /// POST /api/products/{productId}/images
    [HttpPost("{productId:guid}/images")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<IActionResult> AddImage(
        Guid productId,
        [FromBody] AddProductImageRequest request)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return NotFound(new { message = "Product not found." });

        if (product.SellerId != CurrentUserId &&
            !User.IsInRole("Admin"))
            return Forbid();

        // If this is marked primary, unset existing primary
        if (request.IsPrimary)
        {
            var existing = await _db.ProductImages
                .Where(i => i.ProductId == productId && i.IsPrimary)
                .ToListAsync();
            foreach (var img in existing)
                _db.ProductImages.Remove(img);
        }

        var image = ECommerce.ProductService.Domain.Entities
            .ProductImage.Create(
                productId,
                request.Url,
                request.IsPrimary,
                request.SortOrder);

        _db.ProductImages.Add(image);
        await _db.SaveChangesAsync();

        return Ok(new { image.Id, image.Url, image.IsPrimary });
    }

    /// DELETE /api/products/{productId}/images/{imageId}
    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<IActionResult> RemoveImage(
        Guid productId, Guid imageId)
    {
        var image = await _db.ProductImages
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i =>
                i.Id == imageId && i.ProductId == productId);

        if (image is null)
            return NotFound(new { message = "Image not found." });

        if (image.Product.SellerId != CurrentUserId &&
            !User.IsInRole("Admin"))
            return Forbid();

        _db.ProductImages.Remove(image);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Image removed." });
    }

    // ════════════════════════════════════════════════════════
    // ADMIN ENDPOINTS
    // ════════════════════════════════════════════════════════

    /// GET /api/products?page=1&pageSize=20&isApproved=false
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isApproved = null)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsQueryable();

        if (isApproved.HasValue)
            query = query.Where(p => p.IsApproved == isApproved.Value);

        var total = await query.CountAsync();
        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items = products });
    }

    /// PATCH /api/products/{productId}/approve
    [HttpPatch("{productId:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(Guid productId)
    {
        var (success, error) = await _productService
            .ApproveAsync(productId);

        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(new { message = "Product approved and indexed." });
    }

    /// PATCH /api/products/{productId}/activate
    [HttpPatch("{productId:guid}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(Guid productId)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return NotFound(new { message = "Product not found." });

        product.Activate();
        await _db.SaveChangesAsync();

        return Ok(new { message = "Product activated." });
    }
}