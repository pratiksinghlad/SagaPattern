using SagaPatternDemo.Host.Shared.Commands;

namespace SagaPatternDemo.Host.Features.Shipping;

/// <summary>
/// Command representing a shipping started event
/// </summary>
public class ShippingStarted : ICommand
{
    public string OrderId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // ICommand implementation
    public string CorrelationId => OrderId;
    public string MessageType => nameof(ShippingStarted);
}

/// <summary>
/// Command representing a shipping completed event
/// </summary>
public class ShippingCompleted : ICommand
{
    public string OrderId { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // ICommand implementation
    public string CorrelationId => OrderId;
    public string MessageType => nameof(ShippingCompleted);
}