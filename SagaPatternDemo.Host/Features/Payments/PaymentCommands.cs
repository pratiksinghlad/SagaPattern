using SagaPatternDemo.Host.Shared.Commands;

namespace SagaPatternDemo.Host.Features.Payments;

/// <summary>
/// Command representing a successful payment event
/// </summary>
public class PaymentSucceeded : ICommand
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // ICommand implementation
    public string CorrelationId => OrderId;
    public string MessageType => nameof(PaymentSucceeded);
}

/// <summary>
/// Command representing a failed payment event
/// </summary>
public class PaymentFailed : ICommand
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // ICommand implementation
    public string CorrelationId => OrderId;
    public string MessageType => nameof(PaymentFailed);
}