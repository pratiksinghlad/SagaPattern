using Microsoft.Extensions.Logging;
using SagaPatternDemo.Host.Features.Payments;
using SagaPatternDemo.Host.Infrastructure.Data;
using SagaPatternDemo.Host.Infrastructure.ServiceBus;
using SagaPatternDemo.Host.Shared.Configuration;
using SagaPatternDemo.Host.Shared.Handlers;

namespace SagaPatternDemo.Host.Features.Orders;

/// <summary>
/// Handler for OrderCreated commands - initiates payment processing
/// </summary>
public class OrderCreatedHandler : ICommandHandler<OrderCreated>
{
    private readonly IOrderSagaService _sagaService;
    private readonly IServiceBusPublisher _publisher;
    private readonly ServiceBusConfiguration _configuration;
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(
        IOrderSagaService sagaService,
        IServiceBusPublisher publisher,
        ServiceBusConfiguration configuration,
        ILogger<OrderCreatedHandler> logger)
    {
        _sagaService = sagaService ?? throw new ArgumentNullException(nameof(sagaService));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(OrderCreated command, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope("OrderId: {OrderId}", command.OrderId);
        
        _logger.LogInformation("Processing OrderCreated command for amount {Amount}", command.Amount);

        try
        {
            // Create or get existing saga (idempotent)
            var saga = await _sagaService.GetOrCreateSagaAsync(command.OrderId, command.Amount, cancellationToken);

            if (!_sagaService.CanProcessSaga(saga))
            {
                _logger.LogWarning("Saga cannot be processed, current state: {State}", saga.State);
                return;
            }

            // Simulate payment processing by publishing payment events
            // In a real scenario, this would initiate payment processing
            var random = new Random();
            var paymentSucceeds = random.NextDouble() > 0.3; // 70% success rate for demo

            if (paymentSucceeds)
            {
                var paymentSucceeded = new PaymentSucceeded
                {
                    OrderId = command.OrderId,
                    Amount = command.Amount,
                    PaymentId = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                await _publisher.PublishAsync(paymentSucceeded, _configuration.PaymentsQueue, cancellationToken);
                _logger.LogInformation("Published PaymentSucceeded event");
            }
            else
            {
                var paymentFailed = new PaymentFailed
                {
                    OrderId = command.OrderId,
                    Amount = command.Amount,
                    Reason = "Insufficient funds (simulated failure)",
                    CreatedAt = DateTime.UtcNow
                };

                await _publisher.PublishAsync(paymentFailed, _configuration.PaymentsQueue, cancellationToken);
                _logger.LogInformation("Published PaymentFailed event");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreated command");
            throw;
        }
    }
}

/// <summary>
/// Handler for OrderCancelled commands - performs compensation logic
/// </summary>
public class OrderCancelledHandler : ICommandHandler<OrderCancelled>
{
    private readonly IOrderSagaService _sagaService;
    private readonly ILogger<OrderCancelledHandler> _logger;

    public OrderCancelledHandler(
        IOrderSagaService sagaService,
        ILogger<OrderCancelledHandler> logger)
    {
        _sagaService = sagaService ?? throw new ArgumentNullException(nameof(sagaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(OrderCancelled command, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope("OrderId: {OrderId}", command.OrderId);
        
        _logger.LogInformation("Processing OrderCancelled command with reason: {Reason}", command.Reason);

        try
        {
            await _sagaService.CancelSagaAsync(command.OrderId, command.Reason, cancellationToken);
            
            // Here you could add compensation logic like:
            // - Refunding payments
            // - Reversing inventory reservations
            // - Notifying customers
            _logger.LogInformation("Order cancellation completed - compensation logic would run here");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCancelled command");
            throw;
        }
    }
}