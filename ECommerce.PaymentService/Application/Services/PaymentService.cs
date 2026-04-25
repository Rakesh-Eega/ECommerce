// Application/Services/PaymentService.cs
using ECommerce.PaymentService.Application.DTOs;
using ECommerce.PaymentService.Domain.Entities;
using ECommerce.PaymentService.Infrastructure.Messaging;
using ECommerce.PaymentService.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace ECommerce.PaymentService.Application.Services;

public interface IPaymentService
{
    Task<(PaymentIntentDto? Intent, string? Error)> CreatePaymentIntentAsync(
        CreatePaymentIntentRequest request, string customerId);
    Task<(bool Success, string? Error)> HandleWebhookAsync(
        string payload, string signature);
    Task<(bool Success, string? Error)> RefundAsync(
        RefundRequest request);
    Task<PaymentTransactionDto?> GetTransactionByOrderAsync(string orderId);
}

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _db;
    private readonly IPublishEndpoint _bus;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(PaymentDbContext db, IPublishEndpoint bus,
        IConfiguration config, ILogger<PaymentService> logger)
    {
        _db = db;
        _bus = bus;
        _config = config;
        _logger = logger;

        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    public async Task<(PaymentIntentDto?, string?)> CreatePaymentIntentAsync(
        CreatePaymentIntentRequest request, string customerId)
    {
        // Idempotency key — same order always gets same payment intent
        var idempotencyKey = $"order-{request.OrderId}";

        // Check if already created
        var existing = await _db.Transactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);

        if (existing?.PaymentIntentId != null)
        {
            // Return existing intent — prevents duplicate charges
            var existingService = new PaymentIntentService();
            var existingIntent = await existingService
                .GetAsync(existing.PaymentIntentId);

            return (new PaymentIntentDto(
                existingIntent.Id,
                existingIntent.ClientSecret,
                existing.Amount,
                existing.Currency,
                existingIntent.Status), null);
        }

        // Amount in smallest currency unit (paise for INR)
        var amountInPaise = (long)(request.Amount * 100);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInPaise,
            Currency = request.Currency,
            Metadata = new Dictionary<string, string>
            {
                { "orderId",    request.OrderId },
                { "customerId", customerId }
            },
            // Auto payment methods for Indian market
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            }
        };

        var requestOptions = new RequestOptions
        {
            IdempotencyKey = idempotencyKey
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options, requestOptions);

        // Save transaction
        var transaction = PaymentTransaction.Create(
            request.OrderId, customerId,
            request.Amount, idempotencyKey);
        transaction.SetPaymentIntent(intent.Id, intent.ClientSecret);

        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "PaymentIntent created: {IntentId} for Order: {OrderId}",
            intent.Id, request.OrderId);

        return (new PaymentIntentDto(
            intent.Id, intent.ClientSecret,
            request.Amount, request.Currency,
            intent.Status), null);
    }

    public async Task<(bool, string?)> HandleWebhookAsync(
        string payload, string signature)
    {
        var webhookSecret = _config["Stripe:WebhookSecret"]!;

        Stripe.Event stripeEvent;
        try
        {
            // Verify signature — NEVER skip this
            stripeEvent = EventUtility.ConstructEvent(
                payload, signature, webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning("Webhook signature failed: {Error}", ex.Message);
            return (false, "Invalid signature.");
        }

        switch (stripeEvent.Type)
        {
            case "payment_intent.succeeded":
                await HandlePaymentSucceededAsync(
                    stripeEvent.Data.Object as PaymentIntent);
                break;

            case "payment_intent.payment_failed":
                await HandlePaymentFailedAsync(
                    stripeEvent.Data.Object as PaymentIntent);
                break;

            default:
                _logger.LogInformation(
                    "Unhandled Stripe event: {EventType}", stripeEvent.Type);
                break;
        }

        return (true, null);
    }

    public async Task<(bool, string?)> RefundAsync(RefundRequest request)
    {
        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.OrderId == request.OrderId
                && t.Status == "succeeded");

        if (transaction is null)
            return (false, "No successful payment found for this order.");

        var options = new RefundCreateOptions
        {
            PaymentIntent = transaction.PaymentIntentId,
            Reason = request.Reason == "duplicate"
                ? RefundReasons.Duplicate
                : RefundReasons.RequestedByCustomer
        };

        var service = new RefundService();
        var refund = await service.CreateAsync(options);

        if (refund.Status == "succeeded" || refund.Status == "pending")
        {
            transaction.MarkRefunded();
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Refund processed for Order: {OrderId}", request.OrderId);
            return (true, null);
        }

        return (false, "Refund failed.");
    }

    public async Task<PaymentTransactionDto?> GetTransactionByOrderAsync(
        string orderId)
    {
        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.OrderId == orderId);

        if (transaction is null) return null;

        return new PaymentTransactionDto(
            transaction.Id.ToString(),
            transaction.OrderId,
            transaction.PaymentIntentId,
            transaction.Amount,
            transaction.Currency,
            transaction.Status,
            transaction.FailureReason,
            transaction.CreatedAt);
    }

    // ── Private Helpers ──

    private async Task HandlePaymentSucceededAsync(PaymentIntent? intent)
    {
        if (intent is null) return;

        var orderId = intent.Metadata.GetValueOrDefault("orderId");
        if (orderId is null) return;

        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.PaymentIntentId == intent.Id);

        if (transaction is not null)
        {
            transaction.MarkSucceeded();
            await _db.SaveChangesAsync();
        }

        // Notify OrderService
        await _bus.Publish(new PaymentProcessedEvent(
            OrderId: orderId,
            PaymentIntentId: intent.Id,
            IsSuccess: true,
            Amount: intent.Amount / 100m));

        _logger.LogInformation(
            "Payment succeeded for Order: {OrderId}", orderId);
    }

    private async Task HandlePaymentFailedAsync(PaymentIntent? intent)
    {
        if (intent is null) return;

        var orderId = intent.Metadata.GetValueOrDefault("orderId");
        if (orderId is null) return;

        var transaction = await _db.Transactions
            .FirstOrDefaultAsync(t => t.PaymentIntentId == intent.Id);

        if (transaction is not null)
        {
            transaction.MarkFailed(
                intent.LastPaymentError?.Message ?? "Unknown error");
            await _db.SaveChangesAsync();
        }

        // Notify OrderService
        await _bus.Publish(new PaymentProcessedEvent(
            OrderId: orderId,
            PaymentIntentId: intent.Id,
            IsSuccess: false,
            Amount: intent.Amount / 100m));

        _logger.LogInformation(
            "Payment failed for Order: {OrderId}", orderId);
    }
}