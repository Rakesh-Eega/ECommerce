// Controllers/CartController.cs
using ECommerce.CartService.Application.DTOs;
using ECommerce.CartService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.CartService.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
        => _cartService = cartService;

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim missing.");

    // ════════════════════════════════════════════════════════
    // GET ENDPOINTS
    // ════════════════════════════════════════════════════════

    /// GET /api/cart — full cart with all items
    [HttpGet]
    public async Task<IActionResult> GetCart()
        => Ok(await _cartService.GetCartAsync(UserId));

    /// GET /api/cart/summary — lightweight: total + count only
    /// Used on pages that don't need full item details
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var cart = await _cartService.GetCartAsync(UserId);
        return Ok(new
        {
            ItemCount = cart.ItemCount,
            Total = cart.Total
        });
    }

    /// GET /api/cart/count — just the badge number for navbar
    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        var cart = await _cartService.GetCartAsync(UserId);
        return Ok(new { count = cart.ItemCount });
    }

    // ════════════════════════════════════════════════════════
    // ITEM MANAGEMENT
    // ════════════════════════════════════════════════════════

    /// POST /api/cart/items — add item to cart
    [HttpPost("items")]
    public async Task<IActionResult> AddItem(
        [FromBody] AddCartItemRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be at least 1." });

        try
        {
            var cart = await _cartService.AddItemAsync(UserId, request);
            return Ok(cart);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// PATCH /api/cart/items/{variantId} — update quantity
    [HttpPatch("items/{variantId}")]
    public async Task<IActionResult> UpdateQuantity(
        string variantId,
        [FromBody] UpdateQuantityRequest request)
    {
        if (request.Quantity < 0)
            return BadRequest(new { message = "Quantity cannot be negative." });

        var cart = await _cartService
            .UpdateQuantityAsync(UserId, variantId, request.Quantity);
        return Ok(cart);
    }

    /// DELETE /api/cart/items/{variantId} — remove single item
    [HttpDelete("items/{variantId}")]
    public async Task<IActionResult> RemoveItem(string variantId)
    {
        var cart = await _cartService.RemoveItemAsync(UserId, variantId);
        return Ok(cart);
    }

    /// DELETE /api/cart/items — remove multiple items at once
    [HttpDelete("items")]
    public async Task<IActionResult> RemoveItems(
        [FromBody] RemoveItemsRequest request)
    {
        if (request.VariantIds is null || !request.VariantIds.Any())
            return BadRequest(new { message = "No variant IDs provided." });

        var cart = await _cartService
            .RemoveItemsAsync(UserId, request.VariantIds);
        return Ok(cart);
    }

    /// DELETE /api/cart — clear entire cart
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _cartService.ClearCartAsync(UserId);
        return NoContent();
    }

    // ════════════════════════════════════════════════════════
    // CHECKOUT SUPPORT
    // ════════════════════════════════════════════════════════

    /// POST /api/cart/validate
    /// Call this before checkout — checks stock and prices are still valid
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCart()
    {
        var result = await _cartService.ValidateCartAsync(UserId);

        if (!result.IsValid)
            return BadRequest(result);

        return Ok(result);
    }

    // ════════════════════════════════════════════════════════
    // GUEST CART MERGE
    // ════════════════════════════════════════════════════════

    /// POST /api/cart/merge/{guestId}
    /// Call after login — merge guest cart into user cart
    [HttpPost("merge/{guestId}")]
    public async Task<IActionResult> MergeGuestCart(string guestId)
    {
        if (string.IsNullOrWhiteSpace(guestId))
            return BadRequest(new { message = "Guest ID is required." });

        var cart = await _cartService
            .MergeGuestCartAsync(UserId, guestId);
        return Ok(cart);
    }
}