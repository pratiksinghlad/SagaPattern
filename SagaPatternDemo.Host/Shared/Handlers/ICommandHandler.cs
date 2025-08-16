namespace SagaPatternDemo.Host.Shared.Handlers;

/// <summary>
/// Interface for command handlers that process specific command types
/// </summary>
/// <typeparam name="TCommand">The type of command this handler processes</typeparam>
public interface ICommandHandler<in TCommand> where TCommand : class
{
    /// <summary>
    /// Handles the specified command asynchronously
    /// </summary>
    /// <param name="command">The command to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}