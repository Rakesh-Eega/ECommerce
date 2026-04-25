// Application/DTOs/PaymentDtos.cs
namespace ECommerce.PaymentService.Application.DTOs;

public record CreatePaymentIntentRequest(
    string OrderId,
    decimal Amount,
    string Currency = "inr"
);

public record ConfirmPaymentRequest(
    string PaymentIntentId
);

public record RefundRequest(
    string OrderId,
    string? Reason = null
);

public record PaymentIntentDto(
    string PaymentIntentId,
    string ClientSecret,
    decimal Amount,
    string Currency,
    string Status
);

public record PaymentTransactionDto(
    string Id,
    string OrderId,
    string? PaymentIntentId,
    decimal Amount,
    string Currency,
    string Status,
    string? FailureReason,
    DateTime CreatedAt
);