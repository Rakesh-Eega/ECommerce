// Controllers/PaymentController.cs
using ECommerce.PaymentService.Application.DTOs;
using ECommerce.PaymentService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
        => _paymentService = paymentService;

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException();

    // ── Customer ────────────────────────────────────────────

    /// POST /api/payments/create-intent
    /// Call this after creating order — get clientSecret for Stripe.js
    [HttpPost("create-intent")]
    [Authorize]
    public async Task<IActionResult> CreateIntent(
        [FromBody] CreatePaymentIntentRequest request)
    {
        var (intent, error) = await _paymentService
            .CreatePaymentIntentAsync(request, CurrentUserId);

        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(intent);
    }

    /// GET /api/payments/order/{orderId}
    [HttpGet("order/{orderId}")]
    [Authorize]
    public async Task<IActionResult> GetByOrder(string orderId)
    {
        var transaction = await _paymentService
            .GetTransactionByOrderAsync(orderId);

        return transaction is null
            ? NotFound(new { message = "Transaction not found." })
            : Ok(transaction);
    }

    // ── Stripe Webhook — NO Auth, Stripe signs the request ──

    /// POST /api/payments/webhook
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrEmpty(signature))
            return BadRequest(new { message = "Missing Stripe signature." });

        var (success, error) = await _paymentService
            .HandleWebhookAsync(payload, signature);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(); // Always return 200 to Stripe
    }

    // ── Admin ────────────────────────────────────────────────

    /// POST /api/payments/refund
    [HttpPost("refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Refund(
        [FromBody] RefundRequest request)
    {
        var (success, error) = await _paymentService
            .RefundAsync(request);

        if (error is not null)
            return BadRequest(new { message = error });

        return Ok(new { message = "Refund processed." });
    }
}