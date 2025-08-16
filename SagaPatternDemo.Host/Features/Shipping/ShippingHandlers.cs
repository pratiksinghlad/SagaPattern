using Microsoft.Extensions.Logging;
using SagaPatternDemo.Host.Infrastructure.Data;
using SagaPatternDemo.Host.Shared.Handlers;

namespace SagaPatternDemo.Host.Features.Shipping;

/// <summary>
/// Handler for ShippingStarted commands
/// </summary>
public class ShippingStartedHandler : ICommandHandler<ShippingStarted>
{
    private readonly ILogger<ShippingStartedHandler> _logger;

    public ShippingStartedHandler(ILogger<ShippingStartedHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task HandleAsync(ShippingStarted command, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope("OrderId: {OrderId}", command.OrderId);
        
        _logger.LogInformation("Processing ShippingStarted command for address: {Address}", command.Address);

        // In a real implementation, this would:
        // - Contact shipping providers
        // - Generate shipping labels
        // - Update inventory systems
        // - Schedule pickup/delivery

        _logger.LogInformation("Shipping started successfully");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for ShippingCompleted commands - completes the saga
/// </summary>
public class ShippingCompletedHandler : ICommandHandler<ShippingCompleted>
{
    private readonly IOrderSagaService _sagaService;
    private readonly ILogger<ShippingCompletedHandler> _logger;

    public ShippingCompletedHandler(
        IOrderSagaService sagaService,
        ILogger<ShippingCompletedHandler> logger)
    {
        _sagaService = sagaService ?? throw new ArgumentNullException(nameof(sagaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ShippingCompleted command, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope("OrderId: {OrderId}", command.OrderId);
        
        _logger.LogInformation("Processing ShippingCompleted command with tracking: {TrackingNumber}", command.TrackingNumber);

        try
        {
            // Update saga state to completed (idempotent)
            var updated = await _sagaService.ProcessShippingAsync(command.OrderId, cancellationToken);
            
            if (!updated)
            {
                _logger.LogInformation("Shipping already processed or saga cannot be updated");
                return;
            }

            // In a real implementation, this would:
            // - Send tracking information to customer
            // - Update order status in customer systems
            // - Trigger billing/invoicing processes
            // - Generate analytics events

            _logger.LogInformation("Order saga completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ShippingCompleted command");
            throw;
        }
    }
}