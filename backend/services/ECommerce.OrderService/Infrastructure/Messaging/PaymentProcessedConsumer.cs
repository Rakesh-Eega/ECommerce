// Infrastructure/Messaging/PaymentProcessedConsumer.cs
using ECommerce.OrderService.Application.Services;
using MassTransit;

namespace ECommerce.OrderService.Infrastructure.Messaging;

public class PaymentProcessedConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(IOrderService orderService,
        ILogger<PaymentProcessedConsumer> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        _logger.LogInformation(
            "Consumed PaymentProcessedEvent for Order: {OrderId}",
            context.Message.OrderId);

        await _orderService.HandlePaymentProcessedAsync(context.Message);
    }
}