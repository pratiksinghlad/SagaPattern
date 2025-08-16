namespace SagaPatternDemo.Host.Shared.Commands;

/// <summary>
/// Marker interface for all commands in the system
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Unique identifier for the command, used for correlation and idempotency
    /// </summary>
    string CorrelationId { get; }
    
    /// <summary>
    /// Type of the message for routing purposes
    /// </summary>
    string MessageType { get; }
    
    /// <summary>
    /// Timestamp when the command was created
    /// </summary>
    DateTime CreatedAt { get; }
}