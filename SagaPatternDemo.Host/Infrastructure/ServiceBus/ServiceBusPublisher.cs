using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
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
    private readonly ServiceBusAdministrationClient _adminClient;
    private readonly IJsonSerializerProvider _jsonSerializer;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly Dictionary<string, ServiceBusSender> _senders;
    private readonly HashSet<string> _createdQueues;

    public ServiceBusPublisher(
        AzureServiceBusConfiguration configuration,
        IJsonSerializerProvider jsonSerializer,
        ILogger<ServiceBusPublisher> logger)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create Service Bus client with connection string
        _client = new ServiceBusClient(configuration.ConnectionString);
        _adminClient = new ServiceBusAdministrationClient(configuration.ConnectionString);
        _senders = new Dictionary<string, ServiceBusSender>();
        _createdQueues = new HashSet<string>();
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

            var sender = await GetSenderAsync(queueName);
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

    private async Task<ServiceBusSender> GetSenderAsync(string queueName)
    {
        // Ensure queue exists before creating sender
        await EnsureQueueExistsAsync(queueName);

        if (!_senders.TryGetValue(queueName, out var sender))
        {
            sender = _client.CreateSender(queueName);
            _senders[queueName] = sender;
        }
        return sender;
    }

    private async Task EnsureQueueExistsAsync(string queueName)
    {
        if (_createdQueues.Contains(queueName))
            return; // Already checked/created this queue

        try
        {
            // Check if queue exists
            bool queueExists = await _adminClient.QueueExistsAsync(queueName);
            
            if (!queueExists)
            {
                _logger.LogInformation("Creating queue: {QueueName}", queueName);
                
                // Create queue with basic tier compatible options
                var queueOptions = new CreateQueueOptions(queueName)
                {
                    DefaultMessageTimeToLive = TimeSpan.FromDays(14), // Messages expire after 14 days
                    MaxDeliveryCount = 10,
                };

                await _adminClient.CreateQueueAsync(queueOptions);
                _logger.LogInformation("Successfully created queue: {QueueName}", queueName);
            }
            
            _createdQueues.Add(queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure queue exists: {QueueName}", queueName);
            throw;
        }
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