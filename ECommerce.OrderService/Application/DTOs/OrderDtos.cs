// Application/DTOs/OrderDtos.cs
using ECommerce.OrderService.Domain.Enums;

namespace ECommerce.OrderService.Application.DTOs;

// ── Requests ──
public record CreateOrderRequest(
    List<OrderItemRequest> Items,
    ShippingAddressRequest ShippingAddress,
    string? CouponCode = null
);

public record OrderItemRequest(
    string ProductId,
    string VariantId,
    string ProductName,
    string SKU,
    decimal UnitPrice,
    int Quantity,
    string? Size = null,
    string? Color = null,
    string? ImageUrl = null
);

public record ShippingAddressRequest(
    string FullName,
    string Phone,
    string Line1,
    string City,
    string State,
    string PostalCode,
    string? Line2 = null,
    string Country = "India"
);

public record UpdateOrderStatusRequest(
    OrderStatus Status,
    string? Note = null
);

public record CancelOrderRequest(string Reason);

// ── Responses ──
public record OrderDto(
    string Id,
    string OrderNumber,
    string CustomerId,
    string Status,
    string PaymentStatus,
    decimal SubTotal,
    decimal DeliveryCharge,
    decimal Discount,
    decimal Total,
    DateTime CreatedAt,
    ShippingAddressDto Address,
    List<OrderItemDto> Items,
    List<OrderStatusHistoryDto> History
);

public record OrderItemDto(
    string ProductId,
    string VariantId,
    string ProductName,
    string SKU,
    string? Size,
    string? Color,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal SubTotal
);

public record ShippingAddressDto(
    string FullName,
    string Phone,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country
);

public record OrderStatusHistoryDto(
    string Status,
    string? Note,
    DateTime CreatedAt
);

public record OrderSummaryDto(
    string Id,
    string OrderNumber,
    string Status,
    string PaymentStatus,
    decimal Total,
    int ItemCount,
    DateTime CreatedAt,
    string? PrimaryImage
);