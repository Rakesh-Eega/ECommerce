// Infrastructure/Messaging/Events.cs
namespace ECommerce.PaymentService.Infrastructure.Messaging;

// Consumed by PaymentService
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

// Published by PaymentService
public record PaymentProcessedEvent(
    string OrderId,
    string PaymentIntentId,
    bool IsSuccess,
    decimal Amount
);