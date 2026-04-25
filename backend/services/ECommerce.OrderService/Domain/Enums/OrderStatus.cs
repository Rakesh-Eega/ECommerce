// Domain/Enums/OrderStatus.cs
namespace ECommerce.OrderService.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,  // Created, awaiting payment
    Confirmed = 1,  // Payment received
    Processing = 2,  // Being prepared
    Shipped = 3,  // Dispatched
    OutForDelivery = 4,  // Last mile
    Delivered = 5,  // Completed
    Cancelled = 6,  // Cancelled
    Returned = 7,  // Return initiated
    Refunded = 8   // Refund processed
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}