// Infrastructure/Messaging/Events.cs
namespace ECommerce.OrderService.Infrastructure.Messaging;

// Published by OrderService
public record OrderPlacedEvent(
    string OrderId,
    string OrderNumber,
    string CustomerId,
    string CustomerEmail,
    decimal Total,
    DateTime PlacedAt,
    List<OrderPlacedItem> Items
);

public record OrderPlacedItem(
    string ProductId,
    string VariantId,
    int Quantity,
    decimal UnitPrice
);

public record OrderCancelledEvent(
    string OrderId,
    string OrderNumber,
    string CustomerId,
    string Reason,
    DateTime CancelledAt
);

// Consumed by OrderService
public record PaymentProcessedEvent(
    string OrderId,
    string PaymentIntentId,
    bool IsSuccess,
    decimal Amount
);