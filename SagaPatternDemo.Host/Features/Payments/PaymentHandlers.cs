using Microsoft.Extensions.Logging;
using SagaPatternDemo.Host.Features.Orders;
using SagaPatternDemo.Host.Features.Shipping;
using SagaPatternDemo.Host.Infrastructure.Data;
using SagaPatternDemo.Host.Infrastructure.ServiceBus;
using SagaPatternDemo.Host.Shared.Configuration;
using SagaPatternDemo.Host.Shared.Handlers;

namespace SagaPatternDemo.Host.Features.Payments;

/// <summary>
/// Handler for PaymentSucceeded commands - initiates shipping
/// </summary>
public class PaymentSucceededHandler : ICommandHandler<PaymentSucceeded>
{
    private readonly IOrderSagaService _sagaService;
    private readonly IServiceBusPublisher _publisher;
    private readonly ServiceBusConfiguration _configuration;
    private readonly ILogger<PaymentSucceededHandler> _logger;

    public PaymentSucceededHandler(
        IOrderSagaService sagaService,
        IServiceBusPublisher publisher,
        ServiceBusConfiguration configuration,
        ILogger<PaymentSucceededHandler> logger)
    {
        _sagaService = sagaService ?? throw new ArgumentNullException(nameof(sagaService));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(PaymentSucceeded command, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope("OrderId: {OrderId}", command.OrderId);
        
        _logger.LogInformation("Processing PaymentSucceeded command for PaymentId: {PaymentId}", command.PaymentId);

        try
        {
            // Update saga state (idempotent)
            var updated = await _sagaService.ProcessPaymentAsync(command.OrderId, succeeded: true, cancellationToken: cancellationToken);
            
            if (!updated)
            {
                _logger.LogInformation("Payment already processed or saga cannot be updated");
                return;
            }

            // Initiate shipping
            var shippingStarted = new ShippingStarted
            {
                OrderId = command.OrderId,
                Address = "123 Demo Street, Demo City, DC 12345", // Demo address
                CreatedAt = DateTime.UtcNow
            };

            // Simulate shipping completion immediately for demo purposes
            // In real scenarios, shipping would be a separate process
            var shippingCompleted = new ShippingCompleted
            {
                OrderId = command.OrderId,
                TrackingNumber = $"TRACK-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                CreatedAt = DateTime.UtcNow
            };

            await _publisher.PublishAsync(shippingCompleted, _configuration.PaymentsQueue, cancellationToken);
            _logger.LogInformation("Published ShippingCompleted event with tracking: {TrackingNumber}", shippingCompleted.TrackingNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentSucceeded command");
            throw;
        }
    }
}

/// <summary>
/// Handler for PaymentFailed commands - triggers compensation
/// </summary>
public class PaymentFailedHandler : ICommandHandler<PaymentFailed>
{
    private readonly IOrderSagaService _sagaService;
    private readonly IServiceBusPublisher _publisher;
    private readonly ServiceBusConfiguration _configuration;
    private readonly ILogger<PaymentFailedHandler> _logger;

    public PaymentFailedHandler(
        IOrderSagaService sagaService,
        IServiceBusPublisher publisher,
        ServiceBusConfiguration configuration,
        ILogger<PaymentFailedHandler> logger)
    {
        _sagaService = sagaService ?? throw new ArgumentNullException(nameof(sagaService));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(PaymentFailed command, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope("OrderId: {OrderId}", command.OrderId);
        
        _logger.LogInformation("Processing PaymentFailed command with reason: {Reason}", command.Reason);

        try
        {
            // Update saga state (idempotent)
            var updated = await _sagaService.ProcessPaymentAsync(command.OrderId, succeeded: false, command.Reason, cancellationToken);
            
            if (!updated)
            {
                _logger.LogInformation("Payment failure already processed or saga cannot be updated");
                return;
            }

            // Trigger compensation - cancel the order
            var orderCancelled = new OrderCancelled
            {
                OrderId = command.OrderId,
                Reason = $"Payment failed: {command.Reason}",
                CreatedAt = DateTime.UtcNow
            };

            await _publisher.PublishAsync(orderCancelled, _configuration.OrdersQueue, cancellationToken);
            _logger.LogInformation("Published OrderCancelled event for failed payment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PaymentFailed command");
            throw;
        }
    }
}