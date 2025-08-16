using SagaPatternDemo.Host.Shared.Commands;

namespace SagaPatternDemo.Host.Features.Orders;

/// <summary>
/// Command representing an order creation event
/// </summary>
public class OrderCreated : ICommand
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // ICommand implementation
    public string CorrelationId => OrderId;
    public string MessageType => nameof(OrderCreated);
}

/// <summary>
/// Command representing an order cancellation event
/// </summary>
public class OrderCancelled : ICommand
{
    public string OrderId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // ICommand implementation
    public string CorrelationId => OrderId;
    public string MessageType => nameof(OrderCancelled);
}