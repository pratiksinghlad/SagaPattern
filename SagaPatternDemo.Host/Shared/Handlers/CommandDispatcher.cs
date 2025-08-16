using Microsoft.Extensions.DependencyInjection;
using SagaPatternDemo.Host.Shared.Commands;

namespace SagaPatternDemo.Host.Shared.Handlers;

/// <summary>
/// Dispatcher for routing commands to their appropriate handlers
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatches a command to its registered handler
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch</typeparam>
    /// <param name="command">The command instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) 
        where TCommand : class, ICommand;
}

/// <summary>
/// Implementation of command dispatcher using dependency injection
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) 
        where TCommand : class, ICommand
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        await handler.HandleAsync(command, cancellationToken);
    }
}