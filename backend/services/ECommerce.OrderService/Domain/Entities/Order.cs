// Domain/Entities/Order.cs
using ECommerce.OrderService.Domain.Enums;

namespace ECommerce.OrderService.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pending;
    public string? PaymentIntentId { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DeliveryCharge { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Total { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public ShippingAddress ShippingAddress { get; private set; } = null!;
    public List<OrderItem> Items { get; private set; } = new();
    public List<OrderStatusHistory> StatusHistory { get; private set; } = new();

    private Order() { }

    public static Order Create(Guid customerId,
        ShippingAddress address, List<OrderItem> items,
        decimal deliveryCharge = 0, decimal discount = 0)
    {
        var subTotal = items.Sum(i => i.UnitPrice * i.Quantity);
        var total = subTotal + deliveryCharge - discount;

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            CustomerId = customerId,
            ShippingAddress = address,
            SubTotal = subTotal,
            DeliveryCharge = deliveryCharge,
            Discount = discount,
            Total = total
        };

        order.Items.AddRange(items);
        order.StatusHistory.Add(OrderStatusHistory.Create(
            order.Id, OrderStatus.Pending, "Order created"));

        return order;
    }

    public void ConfirmPayment(string paymentIntentId)
    {
        PaymentIntentId = paymentIntentId;
        PaymentStatus = PaymentStatus.Paid;
        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
        StatusHistory.Add(OrderStatusHistory.Create(
            Id, OrderStatus.Confirmed, "Payment confirmed"));
    }

    public void FailPayment()
    {
        PaymentStatus = PaymentStatus.Failed;
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        StatusHistory.Add(OrderStatusHistory.Create(
            Id, OrderStatus.Cancelled, "Payment failed"));
    }

    public void UpdateStatus(OrderStatus newStatus, string? note = null)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        StatusHistory.Add(OrderStatusHistory.Create(Id, newStatus, note));
    }

    public void Cancel(string reason)
    {
        if (Status >= OrderStatus.Shipped)
            throw new InvalidOperationException(
                "Cannot cancel order that has already shipped.");

        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
        StatusHistory.Add(OrderStatusHistory.Create(
            Id, OrderStatus.Cancelled, $"Cancelled: {reason}"));
    }

    public void Refund()
    {
        PaymentStatus = PaymentStatus.Refunded;
        Status = OrderStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
        StatusHistory.Add(OrderStatusHistory.Create(
            Id, OrderStatus.Refunded, "Refund processed"));
    }

    private static string GenerateOrderNumber()
        => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
}