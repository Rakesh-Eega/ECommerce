// Application/Services/OrderService.cs
using ECommerce.OrderService.Application.DTOs;
using ECommerce.OrderService.Domain.Entities;
using ECommerce.OrderService.Domain.Enums;
using ECommerce.OrderService.Infrastructure.Messaging;
using ECommerce.OrderService.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.OrderService.Application.Services;

public interface IOrderService
{
    Task<(OrderDto? Order, string? Error)> CreateAsync(
        CreateOrderRequest request, Guid customerId);
    Task<OrderDto?> GetByIdAsync(Guid orderId, Guid customerId);
    Task<OrderDto?> GetByOrderNumberAsync(string orderNumber);
    Task<List<OrderSummaryDto>> GetCustomerOrdersAsync(Guid customerId,
        int page = 1, int pageSize = 10);
    Task<(bool, string?)> CancelAsync(Guid orderId,
        Guid customerId, string reason);
    Task<(bool, string?)> UpdateStatusAsync(Guid orderId,
        UpdateOrderStatusRequest request);
    Task HandlePaymentProcessedAsync(
        PaymentProcessedEvent evt);
}

public class OrderService : IOrderService
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<OrderService> _logger;

    public OrderService(OrderDbContext db,
        IPublishEndpoint bus,
        ILogger<OrderService> logger)
    {
        _db = db;
        _bus = bus;
        _logger = logger;
    }

    public async Task<(OrderDto?, string?)> CreateAsync(
        CreateOrderRequest request, Guid customerId)
    {
        if (!request.Items.Any())
            return (null, "Order must have at least one item.");

        // Calculate delivery charge
        var subTotal = request.Items.Sum(i => i.UnitPrice * i.Quantity);
        var deliveryCharge = subTotal >= 499 ? 0 : 49; // Free delivery above ₹499

        // Create order (no DB Id yet — let EF generate)
        var orderId = Guid.NewGuid();

        var address = ShippingAddress.Create(
            orderId,
            request.ShippingAddress.FullName,
            request.ShippingAddress.Phone,
            request.ShippingAddress.Line1,
            request.ShippingAddress.City,
            request.ShippingAddress.State,
            request.ShippingAddress.PostalCode,
            request.ShippingAddress.Line2,
            request.ShippingAddress.Country);

        var items = request.Items.Select(i =>
            OrderItem.Create(orderId, i.ProductId, i.VariantId,
                i.ProductName, i.SKU, i.UnitPrice, i.Quantity,
                i.Size, i.Color, i.ImageUrl)).ToList();

        var order = Order.Create(customerId, address, items,
            deliveryCharge);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order created: {OrderNumber} for Customer: {CustomerId}",
            order.OrderNumber, customerId);

        // Publish event — PaymentService and NotificationService subscribe
        await _bus.Publish(new OrderPlacedEvent(
            OrderId: order.Id.ToString(),
            OrderNumber: order.OrderNumber,
            CustomerId: customerId.ToString(),
            CustomerEmail: string.Empty, // fetch from Identity in production
            Total: order.Total,
            PlacedAt: order.CreatedAt,
            Items: request.Items.Select(i => new OrderPlacedItem(
                i.ProductId, i.VariantId, i.Quantity, i.UnitPrice)).ToList()
        ));

        return (await MapToDto(order), null);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid orderId, Guid customerId)
    {
        var order = await GetFullOrderAsync(orderId);
        if (order is null) return null;

        // Customers can only see their own orders
        if (order.CustomerId != customerId) return null;

        return await MapToDto(order);
    }

    public async Task<OrderDto?> GetByOrderNumberAsync(string orderNumber)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.ShippingAddress)
            .Include(o => o.StatusHistory.OrderBy(h => h.CreatedAt))
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        return order is null ? null : await MapToDto(order);
    }

    public async Task<List<OrderSummaryDto>> GetCustomerOrdersAsync(
        Guid customerId, int page = 1, int pageSize = 10)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.Id.ToString(),
                o.OrderNumber,
                o.Status.ToString(),
                o.PaymentStatus.ToString(),
                o.Total,
                o.Items.Sum(i => i.Quantity),
                o.CreatedAt,
                o.Items.FirstOrDefault() != null
                    ? o.Items.First().ImageUrl
                    : null))
            .ToListAsync();
    }

    public async Task<(bool, string?)> CancelAsync(
        Guid orderId, Guid customerId, string reason)
    {
        var order = await GetFullOrderAsync(orderId);
        if (order is null) return (false, "Order not found.");
        if (order.CustomerId != customerId)
            return (false, "Unauthorized.");

        try
        {
            order.Cancel(reason);
            await _db.SaveChangesAsync();

            await _bus.Publish(new OrderCancelledEvent(
                OrderId: order.Id.ToString(),
                OrderNumber: order.OrderNumber,
                CustomerId: customerId.ToString(),
                Reason: reason,
                CancelledAt: DateTime.UtcNow));

            return (true, null);
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool, string?)> UpdateStatusAsync(
        Guid orderId, UpdateOrderStatusRequest request)
    {
        var order = await GetFullOrderAsync(orderId);
        if (order is null) return (false, "Order not found.");

        order.UpdateStatus(request.Status, request.Note);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} status → {Status}",
            order.OrderNumber, request.Status);

        return (true, null);
    }

    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent evt)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id.ToString() == evt.OrderId);

        if (order is null)
        {
            _logger.LogWarning("PaymentProcessed: Order {OrderId} not found",
                evt.OrderId);
            return;
        }

        if (evt.IsSuccess)
            order.ConfirmPayment(evt.PaymentIntentId);
        else
            order.FailPayment();

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Payment {Result} for Order {OrderNumber}",
            evt.IsSuccess ? "confirmed" : "failed",
            order.OrderNumber);
    }

    // ── Private Helpers ──
    private async Task<Order?> GetFullOrderAsync(Guid id)
        => await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.ShippingAddress)
            .Include(o => o.StatusHistory.OrderBy(h => h.CreatedAt))
            .FirstOrDefaultAsync(o => o.Id == id);

    private static Task<OrderDto> MapToDto(Order o)
        => Task.FromResult(new OrderDto(
            Id: o.Id.ToString(),
            OrderNumber: o.OrderNumber,
            CustomerId: o.CustomerId.ToString(),
            Status: o.Status.ToString(),
            PaymentStatus: o.PaymentStatus.ToString(),
            SubTotal: o.SubTotal,
            DeliveryCharge: o.DeliveryCharge,
            Discount: o.Discount,
            Total: o.Total,
            CreatedAt: o.CreatedAt,
            Address: new ShippingAddressDto(
                o.ShippingAddress.FullName,
                o.ShippingAddress.Phone,
                o.ShippingAddress.Line1,
                o.ShippingAddress.Line2,
                o.ShippingAddress.City,
                o.ShippingAddress.State,
                o.ShippingAddress.PostalCode,
                o.ShippingAddress.Country),
            Items: o.Items.Select(i => new OrderItemDto(
                i.ProductId, i.VariantId, i.ProductName,
                i.SKU, i.Size, i.Color, i.ImageUrl,
                i.UnitPrice, i.Quantity,
                i.UnitPrice * i.Quantity)).ToList(),
            History: o.StatusHistory.Select(h => new OrderStatusHistoryDto(
                h.Status.ToString(), h.Note, h.CreatedAt)).ToList()
        ));
}