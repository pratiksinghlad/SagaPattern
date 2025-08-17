using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SagaPatternDemo.Host.Shared.Configuration;

namespace SagaPatternDemo.Host.Infrastructure.ServiceBus;

/// <summary>
/// Background service that manages Service Bus message processing
/// </summary>
public class ServiceBusBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AzureServiceBusConfiguration _configuration;
    private readonly ILogger<ServiceBusBackgroundService> _logger;

    public ServiceBusBackgroundService(
        IServiceProvider serviceProvider,
        AzureServiceBusConfiguration configuration,
        ILogger<ServiceBusBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ServiceBus Background Service starting...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var subscriber = scope.ServiceProvider.GetRequiredService<IServiceBusSubscriber>();

            // Start processing messages from both queues
            await subscriber.StartProcessingAsync(_configuration.OrdersQueue, stoppingToken);
            await subscriber.StartProcessingAsync(_configuration.PaymentsQueue, stoppingToken);

            _logger.LogInformation("ServiceBus Background Service started successfully. Processing messages from queues: {OrdersQueue}, {PaymentsQueue}",
                _configuration.OrdersQueue, _configuration.PaymentsQueue);

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ServiceBus Background Service stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ServiceBus Background Service encountered an error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ServiceBus Background Service stopping...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var subscriber = scope.ServiceProvider.GetRequiredService<IServiceBusSubscriber>();
            await subscriber.StopProcessingAsync(cancellationToken);
            _logger.LogInformation("ServiceBus Background Service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping ServiceBus Background Service");
        }

        await base.StopAsync(cancellationToken);
    }
}