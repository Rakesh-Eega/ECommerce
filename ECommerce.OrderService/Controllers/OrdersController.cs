// Controllers/OrdersController.cs
using ECommerce.OrderService.Application.DTOs;
using ECommerce.OrderService.Application.Services;
using ECommerce.OrderService.Domain.Enums;
using ECommerce.OrderService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerce.OrderService.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly OrderDbContext _db;           // ← was missing

    public OrdersController(IOrderService orderService,
                             OrderDbContext db)     // ← inject it
    {
        _orderService = orderService;
        _db = db;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());

    // ── Customer ────────────────────────────────────────────

    /// POST /api/orders
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request)
    {
        var (order, error) = await _orderService
            .CreateAsync(request, CurrentUserId);

        if (error is not null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(GetById),
            new { orderId = order!.Id }, order);
    }

    /// GET /api/orders
    [HttpGet]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var orders = await _orderService
            .GetCustomerOrdersAsync(CurrentUserId, page, pageSize);
        return Ok(orders);
    }

    /// GET /api/orders/{orderId}
    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(Guid orderId)
    {
        var order = await _orderService
            .GetByIdAsync(orderId, CurrentUserId);

        return order is null
            ? NotFound(new { message = "Order not found." })
            : Ok(order);
    }

    /// GET /api/orders/number/{orderNumber}
    [HttpGet("number/{orderNumber}")]
    public async Task<IActionResult> GetByOrderNumber(string orderNumber)
    {
        var order = await _orderService
            .GetByOrderNumberAsync(orderNumber);

        return order is null
            ? NotFound(new { message = "Order not found." })
            : Ok(order);
    }

    /// POST /api/orders/{orderId}/cancel
    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid orderId,
        [FromBody] CancelOrderRequest request)
    {
        var (success, error) = await _orderService
            .CancelAsync(orderId, CurrentUserId, request.Reason);

        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(new { message = "Order cancelled." });
    }

    // ── Admin ────────────────────────────────────────────────

    /// GET /api/orders/admin/all?page=1&pageSize=20&status=Pending
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? customerId = null)
    {
        var query = _db.Orders
            .Include(o => o.Items)
            .Include(o => o.ShippingAddress)
            .AsQueryable();

        // Filter by status
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            query = query.Where(o => o.Status == parsedStatus);

        // Filter by customer
        if (!string.IsNullOrWhiteSpace(customerId) &&
            Guid.TryParse(customerId, out var parsedCustomerId))
            query = query.Where(o => o.CustomerId == parsedCustomerId);

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                Id = o.Id.ToString(),
                o.OrderNumber,
                CustomerId = o.CustomerId.ToString(),
                Status = o.Status.ToString(),
                PaymentStatus = o.PaymentStatus.ToString(),
                o.SubTotal,
                o.DeliveryCharge,
                o.Discount,
                o.Total,
                o.CreatedAt,
                ItemCount = o.Items.Sum(i => i.Quantity),
                ShippingTo = o.ShippingAddress.FullName,
                City = o.ShippingAddress.City
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items = orders });
    }

    /// PATCH /api/orders/{orderId}/status
    [HttpPatch("{orderId:guid}/status")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<IActionResult> UpdateStatus(
        Guid orderId,
        [FromBody] UpdateOrderStatusRequest request)
    {
        var (success, error) = await _orderService
            .UpdateStatusAsync(orderId, request);

        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(new { message = $"Order status updated to {request.Status}." });
    }
}