using Microsoft.Extensions.Logging;
using SagaPatternDemo.Host.Infrastructure.ServiceBus;
using SagaPatternDemo.Host.Shared.Configuration;

namespace SagaPatternDemo.Host.Features.Orders;

/// <summary>
/// Utility class for sending order commands (useful for testing and demos)
/// </summary>
public interface IOrderCommandSender
{
    /// <summary>
    /// Sends an OrderCreated command to initiate the saga
    /// </summary>
    /// <param name="orderId">Unique order identifier</param>
    /// <param name="amount">Order amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SendOrderCreatedAsync(string orderId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an OrderCancelled command
    /// </summary>
    /// <param name="orderId">Order identifier</param>
    /// <param name="reason">Cancellation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SendOrderCancelledAsync(string orderId, string reason, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of order command sender
/// </summary>
public class OrderCommandSender : IOrderCommandSender
{
    private readonly IServiceBusPublisher _publisher;
    private readonly AzureServiceBusConfiguration _configuration;
    private readonly ILogger<OrderCommandSender> _logger;

    public OrderCommandSender(
        IServiceBusPublisher publisher,
        AzureServiceBusConfiguration configuration,
        ILogger<OrderCommandSender> logger)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendOrderCreatedAsync(string orderId, decimal amount, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Order ID cannot be null or empty", nameof(orderId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        var command = new OrderCreated
        {
            OrderId = orderId,
            Amount = amount,
            CreatedAt = DateTime.UtcNow
        };

        await _publisher.PublishAsync(command, _configuration.OrdersQueue, cancellationToken);
        
        _logger.LogInformation("Sent OrderCreated command for OrderId: {OrderId}, Amount: {Amount}", orderId, amount);
    }

    public async Task SendOrderCancelledAsync(string orderId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Order ID cannot be null or empty", nameof(orderId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

        var command = new OrderCancelled
        {
            OrderId = orderId,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };

        await _publisher.PublishAsync(command, _configuration.OrdersQueue, cancellationToken);
        
        _logger.LogInformation("Sent OrderCancelled command for OrderId: {OrderId}, Reason: {Reason}", orderId, reason);
    }
}