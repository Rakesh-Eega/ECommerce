// Application/DTOs/CartDtos.cs
namespace ECommerce.CartService.Application.DTOs;

public record AddCartItemRequest(
    string ProductId,
    string VariantId,
    int Quantity
);

public record UpdateQuantityRequest(int Quantity);

public record CartItemDto(
    string ProductId,
    string VariantId,
    string ProductName,
    string? ImageUrl,
    string? Size,
    string? Color,
    decimal Price,
    int Quantity,
    int MaxStock,
    decimal SubTotal
);

public record CartDto(
    string UserId,
    List<CartItemDto> Items,
    decimal Total,
    int ItemCount,
    DateTime UpdatedAt
);

public record RemoveItemsRequest(List<string> VariantIds);

public record CartValidationResult(
    bool IsValid,
    List<string> Issues,       // human-readable issues
    List<string> RemovedItems, // variant IDs removed (out of stock)
    List<string> PriceChanged  // variant IDs whose price changed
);