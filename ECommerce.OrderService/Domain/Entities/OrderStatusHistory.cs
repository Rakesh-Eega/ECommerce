// Domain/Entities/OrderStatusHistory.cs
using ECommerce.OrderService.Domain.Enums;

namespace ECommerce.OrderService.Domain.Entities;

public class OrderStatusHistory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Order Order { get; private set; } = null!;

    private OrderStatusHistory() { }

    public static OrderStatusHistory Create(
        Guid orderId, OrderStatus status, string? note = null)
        => new() { OrderId = orderId, Status = status, Note = note };
}