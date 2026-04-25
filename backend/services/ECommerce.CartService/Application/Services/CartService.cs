// Application/Services/CartService.cs
using ECommerce.CartService.Application.DTOs;
using ECommerce.CartService.Domain;
using ECommerce.CartService.Infrastructure.Cache;

namespace ECommerce.CartService.Application.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);
    Task<CartDto> AddItemAsync(string userId, AddCartItemRequest request);
    Task<CartDto> UpdateQuantityAsync(string userId, string variantId, int quantity);
    Task<CartDto> RemoveItemAsync(string userId, string variantId);
    Task ClearCartAsync(string userId);
    Task<CartDto> MergeGuestCartAsync(string userId, string guestId);
    Task<CartDto> RemoveItemsAsync(string userId, List<string> variantIds);
    Task<CartValidationResult> ValidateCartAsync(string userId);
}

public class CartService : ICartService
{
    private readonly ICartRepository _repo;
    private readonly IProductPriceClient _priceClient; // HTTP to ProductService
    private readonly ILogger<CartService> _logger;

    public CartService(ICartRepository repo,
        IProductPriceClient priceClient,
        ILogger<CartService> logger)
    {
        _repo = repo;
        _priceClient = priceClient;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync(string userId)
    {
        var cart = await _repo.GetOrCreateAsync(userId);
        return MapToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(string userId, AddCartItemRequest request)
    {
        // CRITICAL: fetch price from ProductService — never trust client
        var variantInfo = await _priceClient.GetVariantInfoAsync(request.VariantId);
        if (variantInfo is null)
            throw new InvalidOperationException("Product variant not found.");

        if (variantInfo.StockQuantity <= 0)
            throw new InvalidOperationException("Product is out of stock.");

        var cart = await _repo.GetOrCreateAsync(userId);
        cart.AddItem(new CartItem
        {
            ProductId = request.ProductId,
            VariantId = request.VariantId,
            ProductName = variantInfo.ProductName,
            ImageUrl = variantInfo.ImageUrl,
            Size = variantInfo.Size,
            Color = variantInfo.Color,
            Price = variantInfo.Price,      // Server-side price
            Quantity = request.Quantity,
            MaxStock = variantInfo.StockQuantity
        });

        await _repo.SaveAsync(cart);
        _logger.LogInformation("Item added to cart: {UserId} - {VariantId}",
            userId, request.VariantId);

        return MapToDto(cart);
    }

    public async Task<CartDto> UpdateQuantityAsync(
        string userId, string variantId, int quantity)
    {
        var cart = await _repo.GetOrCreateAsync(userId);
        cart.UpdateQuantity(variantId, quantity);
        await _repo.SaveAsync(cart);
        return MapToDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(string userId, string variantId)
    {
        var cart = await _repo.GetOrCreateAsync(userId);
        cart.RemoveItem(variantId);
        await _repo.SaveAsync(cart);
        return MapToDto(cart);
    }

    public async Task ClearCartAsync(string userId)
        => await _repo.DeleteAsync(userId);

    // Merge guest cart on login
    public async Task<CartDto> MergeGuestCartAsync(string userId, string guestId)
    {
        var guestCart = await _repo.GetAsync(guestId);
        if (guestCart is null || !guestCart.Items.Any())
            return await GetCartAsync(userId);

        var userCart = await _repo.GetOrCreateAsync(userId);
        foreach (var item in guestCart.Items)
            userCart.AddItem(item);

        await _repo.SaveAsync(userCart);
        await _repo.DeleteAsync(guestId);

        return MapToDto(userCart);
    }

    private static CartDto MapToDto(Cart cart) => new(
        UserId: cart.UserId,
        Items: cart.Items.Select(i => new CartItemDto(
            i.ProductId, i.VariantId, i.ProductName,
            i.ImageUrl, i.Size, i.Color,
            i.Price, i.Quantity, i.MaxStock,
            i.Price * i.Quantity)).ToList(),
        Total: cart.Total,
        ItemCount: cart.ItemCount,
        UpdatedAt: cart.UpdatedAt
    );

    public async Task<CartDto> RemoveItemsAsync(
    string userId, List<string> variantIds)
    {
        var cart = await _repo.GetOrCreateAsync(userId);

        foreach (var variantId in variantIds)
            cart.RemoveItem(variantId);

        await _repo.SaveAsync(cart);
        return MapToDto(cart);
    }

    public async Task<CartValidationResult> ValidateCartAsync(string userId)
    {
        var cart = await _repo.GetOrCreateAsync(userId);

        if (!cart.Items.Any())
            return new CartValidationResult(
                IsValid: true,
                Issues: new(),
                RemovedItems: new(),
                PriceChanged: new());

        var issues = new List<string>();
        var removedItems = new List<string>();
        var priceChanged = new List<string>();

        foreach (var item in cart.Items.ToList())
        {
            // Fetch latest info from ProductService
            var variantInfo = await _priceClient
                .GetVariantInfoAsync(item.VariantId);

            // Product no longer exists or is inactive
            if (variantInfo is null)
            {
                issues.Add($"'{item.ProductName}' is no longer available.");
                removedItems.Add(item.VariantId);
                cart.RemoveItem(item.VariantId);
                continue;
            }

            // Out of stock
            if (variantInfo.StockQuantity <= 0)
            {
                issues.Add($"'{item.ProductName}' is out of stock.");
                removedItems.Add(item.VariantId);
                cart.RemoveItem(item.VariantId);
                continue;
            }

            // Requested quantity exceeds stock
            if (item.Quantity > variantInfo.StockQuantity)
            {
                issues.Add($"Only {variantInfo.StockQuantity} units of " +
                           $"'{item.ProductName}' available. Quantity adjusted.");
                cart.UpdateQuantity(item.VariantId, variantInfo.StockQuantity);
            }

            // Price changed
            if (item.Price != variantInfo.Price)
            {
                issues.Add($"Price of '{item.ProductName}' changed " +
                           $"from ₹{item.Price} to ₹{variantInfo.Price}.");
                priceChanged.Add(item.VariantId);

                // Update to latest price
                item.Price = variantInfo.Price;
            }
        }

        // Save cart with any adjustments
        if (issues.Any())
            await _repo.SaveAsync(cart);

        return new CartValidationResult(
            IsValid: !issues.Any(),
            Issues: issues,
            RemovedItems: removedItems,
            PriceChanged: priceChanged
        );
    }
}