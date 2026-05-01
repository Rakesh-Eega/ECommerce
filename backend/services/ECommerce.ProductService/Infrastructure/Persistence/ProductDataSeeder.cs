// Infrastructure/Persistence/ProductDataSeeder.cs
using ECommerce.ProductService.Application.Services;

namespace ECommerce.ProductService.Infrastructure.Persistence;

public class ProductDataSeeder
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductDataSeeder> _logger;

    public ProductDataSeeder(
        IProductService productService,
        ILogger<ProductDataSeeder> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var count = await _productService.ReIndexAllProductsAsync();
        _logger.LogInformation(
            "✅ ProductDataSeeder completed. {Count} products indexed.", count);
    }
}