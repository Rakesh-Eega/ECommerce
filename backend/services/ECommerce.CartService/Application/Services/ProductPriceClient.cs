// Application/Services/ProductPriceClient.cs
using System.Text.Json;

namespace ECommerce.CartService.Application.Services;

public record VariantInfo(
    string ProductName,
    decimal Price,
    int StockQuantity,
    string? ImageUrl,
    string? Size,
    string? Color
);

public interface IProductPriceClient
{
    Task<VariantInfo?> GetVariantInfoAsync(string variantId);
}

public class ProductPriceClient : IProductPriceClient
{
    private readonly HttpClient _http;

    public ProductPriceClient(HttpClient http)
        => _http = http;

    public async Task<VariantInfo?> GetVariantInfoAsync(string variantId)
    {
        try
        {
            var response = await _http.GetAsync($"/api/products/variants/{variantId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<VariantInfo>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ProductPriceClient error: {ex.Message}");
            return null;
        }
    }
}