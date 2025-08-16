using Microsoft.Extensions.Logging;
using SagaPatternDemo.Host.Infrastructure.Data;
using SagaPatternDemo.Host.Shared.Models;

namespace SagaPatternDemo.Host.Infrastructure.Data;

/// <summary>
/// Service for managing order saga state transitions and business logic
/// </summary>
public interface IOrderSagaService
{
    /// <summary>
    /// Creates or gets an existing order saga
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="amount">The order amount (required for new sagas)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The order saga</returns>
    Task<OrderSaga> GetOrCreateSagaAsync(string orderId, decimal? amount = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks payment as processed for the saga (idempotent operation)
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="succeeded">Whether the payment succeeded</param>
    /// <param name="errorMessage">Error message if payment failed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the saga state was updated, false if already processed</returns>
    Task<bool> ProcessPaymentAsync(string orderId, bool succeeded, string? errorMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks shipping as processed for the saga (idempotent operation)
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the saga state was updated, false if already processed</returns>
    Task<bool> ProcessShippingAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the saga as cancelled
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="reason">Cancellation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated saga</returns>
    Task<OrderSaga> CancelSagaAsync(string orderId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the saga is in a state that allows processing
    /// </summary>
    /// <param name="saga">The saga to check</param>
    /// <returns>True if processing can continue</returns>
    bool CanProcessSaga(OrderSaga saga);
}

/// <summary>
/// Implementation of order saga service
/// </summary>
public class OrderSagaService : IOrderSagaService
{
    private readonly ISagaRepository _sagaRepository;
    private readonly ILogger<OrderSagaService> _logger;

    public OrderSagaService(ISagaRepository sagaRepository, ILogger<OrderSagaService> logger)
    {
        _sagaRepository = sagaRepository ?? throw new ArgumentNullException(nameof(sagaRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OrderSaga> GetOrCreateSagaAsync(string orderId, decimal? amount = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Order ID cannot be null or empty", nameof(orderId));

        using var scope = _logger.BeginScope("OrderId: {OrderId}", orderId);

        var existingSaga = await _sagaRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (existingSaga != null)
        {
            _logger.LogDebug("Found existing saga with state {State}", existingSaga.State);
            return existingSaga;
        }

        if (!amount.HasValue)
            throw new ArgumentException("Amount is required when creating a new saga", nameof(amount));

        var newSaga = new OrderSaga
        {
            OrderId = orderId,
            Amount = amount.Value,
            State = SagaState.Created,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _sagaRepository.SaveAsync(newSaga, cancellationToken);
        _logger.LogInformation("Created new saga with amount {Amount}", amount.Value);

        return newSaga;
    }

    public async Task<bool> ProcessPaymentAsync(string orderId, bool succeeded, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Order ID cannot be null or empty", nameof(orderId));

        using var scope = _logger.BeginScope("OrderId: {OrderId}", orderId);

        var saga = await _sagaRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (saga == null)
        {
            _logger.LogWarning("Saga not found for payment processing");
            return false;
        }

        // Idempotency check
        if (saga.PaymentProcessed)
        {
            _logger.LogInformation("Payment already processed for saga with state {State}", saga.State);
            return false;
        }

        if (!CanProcessSaga(saga))
        {
            _logger.LogWarning("Cannot process payment for saga in state {State}", saga.State);
            return false;
        }

        saga.PaymentProcessed = true;
        saga.State = succeeded ? SagaState.PaymentSucceeded : SagaState.PaymentFailed;
        saga.ErrorMessage = errorMessage;

        await _sagaRepository.SaveAsync(saga, cancellationToken);
        _logger.LogInformation("Payment processed: {Succeeded}, new state: {State}", succeeded, saga.State);

        return true;
    }

    public async Task<bool> ProcessShippingAsync(string orderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Order ID cannot be null or empty", nameof(orderId));

        using var scope = _logger.BeginScope("OrderId: {OrderId}", orderId);

        var saga = await _sagaRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (saga == null)
        {
            _logger.LogWarning("Saga not found for shipping processing");
            return false;
        }

        // Idempotency check
        if (saga.ShippingProcessed)
        {
            _logger.LogInformation("Shipping already processed for saga");
            return false;
        }

        // Ensure payment succeeded before shipping
        if (saga.State != SagaState.PaymentSucceeded)
        {
            _logger.LogWarning("Cannot process shipping for saga in state {State}", saga.State);
            return false;
        }

        saga.ShippingProcessed = true;
        saga.State = SagaState.Completed;

        await _sagaRepository.SaveAsync(saga, cancellationToken);
        _logger.LogInformation("Shipping processed, saga completed");

        return true;
    }

    public async Task<OrderSaga> CancelSagaAsync(string orderId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Order ID cannot be null or empty", nameof(orderId));

        using var scope = _logger.BeginScope("OrderId: {OrderId}", orderId);

        var saga = await _sagaRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (saga == null)
            throw new InvalidOperationException($"Saga not found for order {orderId}");

        saga.State = SagaState.Cancelled;
        saga.ErrorMessage = reason;

        await _sagaRepository.SaveAsync(saga, cancellationToken);
        _logger.LogInformation("Saga cancelled with reason: {Reason}", reason);

        return saga;
    }

    public bool CanProcessSaga(OrderSaga saga)
    {
        if (saga == null)
            return false;

        return saga.State != SagaState.Completed && 
               saga.State != SagaState.Failed && 
               saga.State != SagaState.Cancelled;
    }
}