using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SagaPatternDemo.Host.Features.Orders;
using SagaPatternDemo.Host.Features.Payments;
using SagaPatternDemo.Host.Features.Shipping;
using SagaPatternDemo.Host.Infrastructure.Data;
using SagaPatternDemo.Host.Infrastructure.Messaging;
using SagaPatternDemo.Host.Infrastructure.ServiceBus;
using SagaPatternDemo.Host.Shared.Configuration;
using SagaPatternDemo.Host.Shared.Handlers;
using System.Linq;

namespace SagaPatternDemo.Host;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Ensure database is created and migrations are applied
        using (var scope = host.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<SagaDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        // Demo: Send a test order if requested
        if (args.Contains("--send-test-order"))
        {
            await SendTestOrder(host.Services);
        }

        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration - Azure Service Bus configuration using ConnectionStrings
                var azureServiceBusConfig = new AzureServiceBusConfiguration();
                context.Configuration.GetSection("ConnectionStrings:Asb").Bind(azureServiceBusConfig);
                services.AddSingleton(azureServiceBusConfig);

                var databaseConfig = new DatabaseConfiguration();
                context.Configuration.GetSection(DatabaseConfiguration.SectionName).Bind(databaseConfig);
                services.AddSingleton(databaseConfig);

                // Database
                services.AddDbContext<SagaDbContext>(options =>
                {
                    // Extract data source from connection string
                    var connectionStringParts = databaseConfig.ConnectionString.Split(';');
                    var dataSourcePart = connectionStringParts.FirstOrDefault(p => p.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));
                    var dataSource = dataSourcePart?.Split('=')[1] ?? "SagaPatternDemo.db";
                    
                    // Build connection string with SQLCipher password
                    var connectionString = SqlCipherExtensions.BuildSqlCipherConnectionString(
                        dataSource: dataSource,
                        password: databaseConfig.Password);
                    
                    options.UseSqlCipher(connectionString);
                });

                // Infrastructure services
                services.AddSingleton<IJsonSerializerProvider, JsonSerializerProvider>();
                services.AddScoped<ISagaRepository, SagaRepository>();
                services.AddScoped<IOrderSagaService, OrderSagaService>();
                services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();
                services.AddScoped<IServiceBusSubscriber, ServiceBusSubscriber>();

                // Command handlers
                services.AddScoped<ICommandDispatcher, CommandDispatcher>();
                services.AddScoped<ICommandHandler<OrderCreated>, OrderCreatedHandler>();
                services.AddScoped<ICommandHandler<OrderCancelled>, OrderCancelledHandler>();
                services.AddScoped<ICommandHandler<PaymentSucceeded>, PaymentSucceededHandler>();
                services.AddScoped<ICommandHandler<PaymentFailed>, PaymentFailedHandler>();
                services.AddScoped<ICommandHandler<ShippingStarted>, ShippingStartedHandler>();
                services.AddScoped<ICommandHandler<ShippingCompleted>, ShippingCompletedHandler>();

                // Utility services
                services.AddScoped<IOrderCommandSender, OrderCommandSender>();

                // Background service
                services.AddHostedService<ServiceBusBackgroundService>();

                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                });
            });

    private static async Task SendTestOrder(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var orderSender = scope.ServiceProvider.GetRequiredService<IOrderCommandSender>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var orderId = $"ORDER-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        var amount = new Random().Next(50, 1000);

        logger.LogInformation("Sending test order: {OrderId} for amount: ${Amount}", orderId, amount);
        
        try
        {
            await orderSender.SendOrderCreatedAsync(orderId, amount);
            logger.LogInformation("Test order sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send test order");
        }
    }
}
