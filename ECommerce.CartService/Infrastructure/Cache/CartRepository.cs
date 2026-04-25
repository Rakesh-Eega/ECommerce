// Infrastructure/Cache/CartRepository.cs
using System.Text.Json;
using ECommerce.CartService.Domain;
using StackExchange.Redis;

namespace ECommerce.CartService.Infrastructure.Cache;

public interface ICartRepository
{
    Task<Cart?> GetAsync(string userId);
    Task SaveAsync(Cart cart);
    Task DeleteAsync(string userId);
    Task<Cart> GetOrCreateAsync(string userId);
}

public class CartRepository : ICartRepository
{
    private readonly IDatabase _redis;
    private const int CartTtlDays = 7;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CartRepository(IConnectionMultiplexer redis)
        => _redis = redis.GetDatabase();

    private static string Key(string userId) => $"cart:{userId}";

    public async Task<Cart?> GetAsync(string userId)
    {
        var data = await _redis.StringGetAsync(Key(userId));
        if (data.IsNullOrEmpty) return null;

        return JsonSerializer.Deserialize<Cart>(data!, JsonOptions);
    }

    public async Task<Cart> GetOrCreateAsync(string userId)
        => await GetAsync(userId) ?? new Cart { UserId = userId };

    public async Task SaveAsync(Cart cart)
    {
        var json = JsonSerializer.Serialize(cart, JsonOptions);
        await _redis.StringSetAsync(
            Key(cart.UserId),
            json,
            TimeSpan.FromDays(CartTtlDays));
    }

    public async Task DeleteAsync(string userId)
        => await _redis.KeyDeleteAsync(Key(userId));
}