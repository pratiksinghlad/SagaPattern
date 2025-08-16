namespace SagaPatternDemo.Host.Shared.Models;

/// <summary>
/// Represents the state of an order saga
/// </summary>
public class OrderSaga
{
    /// <summary>
    /// Unique identifier for the order (correlation ID)
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Order amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Current state of the saga
    /// </summary>
    public SagaState State { get; set; } = SagaState.Created;

    /// <summary>
    /// Flag indicating if payment has been processed
    /// </summary>
    public bool PaymentProcessed { get; set; }

    /// <summary>
    /// Flag indicating if shipping has been processed
    /// </summary>
    public bool ShippingProcessed { get; set; }

    /// <summary>
    /// Timestamp when the saga was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the saga was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if the saga failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Possible states of an order saga
/// </summary>
public enum SagaState
{
    Created,
    PaymentSucceeded,
    PaymentFailed,
    Completed,
    Failed,
    Cancelled
}