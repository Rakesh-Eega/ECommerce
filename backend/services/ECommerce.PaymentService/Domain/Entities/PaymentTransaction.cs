// Domain/Entities/PaymentTransaction.cs
namespace ECommerce.PaymentService.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string OrderId { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public string? PaymentIntentId { get; private set; }
    public string? ClientSecret { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "inr";
    public string Status { get; private set; } = "pending";
    public string? FailureReason { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private PaymentTransaction() { }

    public static PaymentTransaction Create(string orderId,
        string customerId, decimal amount, string idempotencyKey)
        => new()
        {
            OrderId = orderId,
            CustomerId = customerId,
            Amount = amount,
            IdempotencyKey = idempotencyKey
        };

    public void SetPaymentIntent(string intentId, string clientSecret)
    {
        PaymentIntentId = intentId;
        ClientSecret = clientSecret;
        Status = "created";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSucceeded()
    {
        Status = "succeeded";
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = "failed";
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRefunded()
    {
        Status = "refunded";
        UpdatedAt = DateTime.UtcNow;
    }
}