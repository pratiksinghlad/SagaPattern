using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using SagaPatternDemo.Host.Infrastructure.Messaging;
using SagaPatternDemo.Host.Shared.Commands;
using SagaPatternDemo.Host.Shared.Configuration;

namespace SagaPatternDemo.Host.Infrastructure.ServiceBus;

/// <summary>
/// Interface for publishing messages to Azure Service Bus
/// </summary>
public interface IServiceBusPublisher
{
    /// <summary>
    /// Publishes a command to the specified queue
    /// </summary>
    /// <typeparam name="TCommand">The command type</typeparam>
    /// <param name="command">The command to publish</param>
    /// <param name="queueName">The target queue name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task PublishAsync<TCommand>(TCommand command, string queueName, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand;
}

/// <summary>
/// Azure Service Bus publisher implementation
/// </summary>
public class ServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly IJsonSerializerProvider _jsonSerializer;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly Dictionary<string, ServiceBusSender> _senders;

    public ServiceBusPublisher(
        ServiceBusConfiguration configuration,
        IJsonSerializerProvider jsonSerializer,
        ILogger<ServiceBusPublisher> logger)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create Service Bus client with Service Principal authentication
        var credential = new ClientSecretCredential(
            configuration.TenantId,
            configuration.ClientId,
            configuration.ClientSecret);

        var fullyQualifiedNamespace = configuration.Namespace.Contains(".servicebus.windows.net")
            ? configuration.Namespace
            : $"{configuration.Namespace}.servicebus.windows.net";

        _client = new ServiceBusClient(fullyQualifiedNamespace, credential);
        _senders = new Dictionary<string, ServiceBusSender>();
    }

    public async Task PublishAsync<TCommand>(TCommand command, string queueName, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));

        try
        {
            using var scope = _logger.BeginScope("Publishing message {MessageType} to {QueueName} for OrderId {OrderId}",
                command.MessageType, queueName, command.CorrelationId);

            var sender = GetSender(queueName);
            var messageBody = _jsonSerializer.Serialize(command);

            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = command.CorrelationId,
                Subject = command.MessageType,
                ContentType = "application/json"
            };

            // Add custom properties
            serviceBusMessage.ApplicationProperties["MessageType"] = command.MessageType;
            serviceBusMessage.ApplicationProperties["CreatedAt"] = command.CreatedAt.ToString("O");

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

            _logger.LogInformation("Successfully published message {MessageType} to {QueueName} for OrderId {OrderId}",
                command.MessageType, queueName, command.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message {MessageType} to {QueueName} for OrderId {OrderId}",
                command.MessageType, queueName, command.CorrelationId);
            throw;
        }
    }

    private ServiceBusSender GetSender(string queueName)
    {
        if (!_senders.TryGetValue(queueName, out var sender))
        {
            sender = _client.CreateSender(queueName);
            _senders[queueName] = sender;
        }
        return sender;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
        _senders.Clear();
        await _client.DisposeAsync();
    }
}