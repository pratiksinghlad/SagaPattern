using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using SagaPatternDemo.Host.Infrastructure.Messaging;
using SagaPatternDemo.Host.Shared.Commands;
using SagaPatternDemo.Host.Shared.Configuration;
using SagaPatternDemo.Host.Shared.Handlers;

namespace SagaPatternDemo.Host.Infrastructure.ServiceBus;

/// <summary>
/// Interface for Service Bus message processing
/// </summary>
public interface IServiceBusSubscriber
{
    /// <summary>
    /// Starts processing messages from the specified queue
    /// </summary>
    /// <param name="queueName">The queue to process messages from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StartProcessingAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops processing messages
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StopProcessingAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Azure Service Bus subscriber implementation
/// </summary>
public class ServiceBusSubscriber : IServiceBusSubscriber, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusAdministrationClient _adminClient;
    private readonly IJsonSerializerProvider _jsonSerializer;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly ILogger<ServiceBusSubscriber> _logger;
    private readonly AzureServiceBusConfiguration _configuration;
    private readonly Dictionary<string, ServiceBusProcessor> _processors;
    private readonly HashSet<string> _createdQueues;

    public ServiceBusSubscriber(
        AzureServiceBusConfiguration configuration,
        IJsonSerializerProvider jsonSerializer,
        ICommandDispatcher commandDispatcher,
        ILogger<ServiceBusSubscriber> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create Service Bus client with connection string
        _client = new ServiceBusClient(configuration.ConnectionString);
        _adminClient = new ServiceBusAdministrationClient(configuration.ConnectionString);
        _processors = new Dictionary<string, ServiceBusProcessor>();
        _createdQueues = new HashSet<string>();
    }

    public async Task StartProcessingAsync(string queueName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));

        try
        {
            // Ensure queue exists before creating processor
            await EnsureQueueExistsAsync(queueName);
            
            var processor = GetProcessor(queueName);
            
            processor.ProcessMessageAsync += ProcessMessageAsync;
            processor.ProcessErrorAsync += ProcessErrorAsync;

            await processor.StartProcessingAsync(cancellationToken);

            _logger.LogInformation("Started processing messages from queue: {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start processing messages from queue: {QueueName}", queueName);
            throw;
        }
    }

    public async Task StopProcessingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stopTasks = _processors.Values.Select(p => p.StopProcessingAsync(cancellationToken));
            await Task.WhenAll(stopTasks);

            _logger.LogInformation("Stopped processing messages from all queues");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping message processing");
            throw;
        }
    }

    private ServiceBusProcessor GetProcessor(string queueName)
    {
        if (!_processors.TryGetValue(queueName, out var processor))
        {
            var options = new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = _configuration.MaxConcurrentCalls,
                AutoCompleteMessages = false, // We'll handle completion manually
                ReceiveMode = ServiceBusReceiveMode.PeekLock
            };

            processor = _client.CreateProcessor(queueName, options);
            _processors[queueName] = processor;
        }
        return processor;
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

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageType = args.Message.ApplicationProperties.TryGetValue("MessageType", out var msgType) 
            ? msgType?.ToString() 
            : args.Message.Subject;

        var correlationId = args.Message.CorrelationId;

        try
        {
            using var scope = _logger.BeginScope("Processing message {MessageType} for OrderId {OrderId}",
                messageType, correlationId);

            _logger.LogInformation("Received message {MessageType} from queue for OrderId {OrderId}",
                messageType, correlationId);

            var messageBody = args.Message.Body.ToString();
            var command = await DeserializeCommandAsync(messageBody, messageType);

            if (command != null)
            {
                await _commandDispatcher.DispatchAsync(command, args.CancellationToken);
                await args.CompleteMessageAsync(args.Message, args.CancellationToken);

                _logger.LogInformation("Successfully processed message {MessageType} for OrderId {OrderId}",
                    messageType, correlationId);
            }
            else
            {
                _logger.LogWarning("Unknown message type {MessageType}, dead-lettering message", messageType);
                await args.DeadLetterMessageAsync(args.Message, "Unknown message type", 
                    $"Cannot process message of type {messageType}", args.CancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageType} for OrderId {OrderId}",
                messageType, correlationId);

            // For this demo, we'll dead-letter failed messages
            // In production, you might want to implement retry logic
            await args.DeadLetterMessageAsync(args.Message, "Processing failed", ex.Message, args.CancellationToken);
        }
    }

    private async Task<ICommand?> DeserializeCommandAsync(string messageBody, string? messageType)
    {
        return messageType switch
        {
            "OrderCreated" => _jsonSerializer.Deserialize<Features.Orders.OrderCreated>(messageBody),
            "OrderCancelled" => _jsonSerializer.Deserialize<Features.Orders.OrderCancelled>(messageBody),
            "PaymentSucceeded" => _jsonSerializer.Deserialize<Features.Payments.PaymentSucceeded>(messageBody),
            "PaymentFailed" => _jsonSerializer.Deserialize<Features.Payments.PaymentFailed>(messageBody),
            "ShippingStarted" => _jsonSerializer.Deserialize<Features.Shipping.ShippingStarted>(messageBody),
            "ShippingCompleted" => _jsonSerializer.Deserialize<Features.Shipping.ShippingCompleted>(messageBody),
            _ => null
        };
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error processing message from queue {QueueName}: {ErrorSource}",
            args.EntityPath, args.ErrorSource);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var processor in _processors.Values)
        {
            await processor.DisposeAsync();
        }
        _processors.Clear();
        await _client.DisposeAsync();
    }
}