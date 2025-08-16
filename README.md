# Saga Pattern Demo with Azure Service Bus

A complete .NET 8 C# implementation demonstrating the Saga Pattern using Azure Service Bus for distributed transaction orchestration. This project implements an order processing workflow with payment and shipping handling, including compensation logic for failures.

## 🏗️ Architecture Overview

This solution implements the **Saga Pattern** using the **orchestration approach** with Azure Service Bus as the message broker. The saga manages the order lifecycle:

1. **Order Creation** → Triggers payment processing
2. **Payment Processing** → On success, triggers shipping; on failure, triggers cancellation
3. **Shipping Processing** → Completes the order saga
4. **Compensation Logic** → Handles failures and rollbacks

### Saga Pattern Flow

![Saga Pattern Flow Diagram](./saga-pattern-flow.svg)

Order ▶ Payment ▶ Shipping with compensation on failure, orchestrated via Azure Service Bus; state persisted via EF Core.

Key components: ServiceBusPublisher/Subscriber, CommandDispatcher, OrderSagaService, EF Core DbContext.

Relevant code: `Features/*/*Handlers.cs`, `Shared/Commands`, `Infrastructure/ServiceBus`, `Infrastructure/Data`.

## 🚀 Features

- ✅ **Saga Pattern Implementation** with idempotency and correlation
- ✅ **Azure Service Bus Integration** with Service Principal authentication
- ✅ **Command/Handler Pattern** with dependency injection
- ✅ **EF Core Persistence** for saga state management
- ✅ **Background Service** for continuous message processing
- ✅ **Comprehensive Logging** with correlation scopes
- ✅ **Unit Tests** with mocking and in-memory databases
- ✅ **Production-ready Code** following SOLID principles
- ✅ **Graceful Shutdown** and error handling

## 📁 Project Structure

```text
SagaPatternDemo/
├── SagaPatternDemo.Host/              # Main console application
│   ├── Features/                      # Feature-based organization
│   │   ├── Orders/                    # Order-related commands and handlers
│   │   ├── Payments/                  # Payment processing logic
│   │   └── Shipping/                  # Shipping management
│   ├── Infrastructure/                # Cross-cutting concerns
│   │   ├── Data/                      # EF Core DbContext and repositories
│   │   ├── Messaging/                 # JSON serialization provider
│   │   └── ServiceBus/                # Azure Service Bus integration
│   ├── Shared/                        # Shared abstractions
│   │   ├── Commands/                  # Command interfaces
│   │   ├── Configuration/             # Configuration classes
│   │   ├── Handlers/                  # Handler interfaces and dispatcher
│   │   └── Models/                    # Domain models
│   └── Migrations/                    # EF Core migrations
└── SagaPatternDemo.Tests/             # Unit tests
```

## 🛠️ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (included with Visual Studio)
- Azure Service Bus namespace with Service Principal access
- Azure subscription for Service Bus (or Azure Service Bus Emulator for local testing)

## ⚙️ Configuration

### 1. Azure Service Bus Setup

Create an Azure Service Bus namespace and configure Service Principal authentication:

1. Create a Service Bus namespace in Azure Portal
2. Create two queues: `orders` and `payments`
3. Create a Service Principal (App Registration) in Azure AD
4. Grant the Service Principal appropriate permissions on the Service Bus namespace

### 2. Application Configuration

Update `appsettings.json` with your Azure Service Bus details:

```json
{
  "ServiceBus": {
    "Namespace": "your-servicebus-namespace.servicebus.windows.net",
    "TenantId": "your-azure-tenant-id",
    "ClientId": "your-service-principal-client-id",
    "ClientSecret": "your-service-principal-client-secret",
    "OrdersQueue": "orders",
    "PaymentsQueue": "payments",
    "MaxConcurrentCalls": 1
  },
  "Database": {
    "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=SagaPatternDemo;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

> **Security Note**: Never commit secrets to source control. Use Azure Key Vault, environment variables, or user secrets for production deployments.

### 3. Database Setup

The application uses Entity Framework Core with SQL Server LocalDB. The database will be created automatically on first run, or you can create it manually:

```bash
# Navigate to the Host project directory
cd SagaPatternDemo.Host

# Create/update database with migrations
dotnet ef database update
```

## 🚀 Running the Application

### Build and Run

```bash
# Navigate to solution directory
cd SagaPatternDemo

# Restore packages
dotnet restore

# Build solution
dotnet build

# Run the application
dotnet run --project SagaPatternDemo.Host
```

### Run with Test Order

To automatically send a test order on startup:

```bash
dotnet run --project SagaPatternDemo.Host -- --send-test-order
```

This will:

1. Start the background service
2. Send a test order with a random amount
3. Process the order through the complete saga workflow

## 🧪 Testing the Workflow

### Manual Testing

You can test the saga workflow by using the built-in order sender. The application includes a simple way to trigger orders:

1. **Start the application** - The background service will begin listening for messages
2. **Send a test order** - Use the `--send-test-order` flag or modify `Program.cs` to send custom orders
3. **Monitor the logs** - Watch the console output to see the saga progression

### Expected Log Flow

```text
[INFO] Sending test order: ORDER-20241209-143022-A1B2C3D4 for amount: $542
[INFO] Published OrderCreated event
[INFO] Processing OrderCreated command for amount 542
[INFO] Processing ShippingCompleted command with tracking: TRACK-E5F6G7H8
[INFO] Shipping processed, saga completed
[INFO] Order saga completed successfully
```

### Unit Tests

Run the comprehensive unit test suite:

````bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "SagaRepositoryTests"
```text

Test coverage includes:

- Saga repository operations
- Saga service state transitions
- JSON serialization/deserialization
- Idempotency validation
- Error handling scenarios

## 🔧 Configuration Options

### Service Bus Settings

- **MaxConcurrentCalls**: Number of concurrent message processors (default: 1 for sequential processing)
- **OrdersQueue**: Queue name for order events
- **PaymentsQueue**: Queue name for payment events

### Logging Configuration

The application uses structured logging with correlation IDs. Log levels can be configured in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SagaPatternDemo.Host": "Debug",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
````

## 🏗️ Database Migrations

### Creating New Migrations

```bash
cd SagaPatternDemo.Host
dotnet ef migrations add <MigrationName>
```

### Applying Migrations

```bash
# Update to latest migration
dotnet ef database update

# Update to specific migration
dotnet ef database update <MigrationName>

# Generate SQL script
dotnet ef migrations script
```

## 🔍 Monitoring and Troubleshooting

### Common Issues

1. **Service Bus Connection Errors**

   - Verify Service Principal credentials
   - Check network connectivity to Azure
   - Ensure queues exist in the namespace

2. **Database Connection Errors**

   - Verify LocalDB is installed and running
   - Check connection string format
   - Ensure user has database creation permissions

3. **Message Processing Errors**
   - Check dead-letter queues for failed messages
   - Review application logs for handler exceptions
   - Verify message format and correlation IDs

### Service Bus Monitoring

Monitor your Service Bus namespace in Azure Portal:

- **Active Messages**: Current messages in queues
- **Dead Letter Messages**: Failed messages requiring attention
- **Throughput**: Messages per second metrics

## 🧩 Extending the Solution

### Adding New Commands

1. Create command class implementing `ICommand`
2. Create corresponding handler implementing `ICommandHandler<T>`
3. Register handler in `Program.cs` DI container
4. Add deserialization case in `ServiceBusSubscriber`

### Adding New Saga Steps

1. Update `OrderSaga` model with new state flags
2. Extend `IOrderSagaService` with new transition methods
3. Create handlers for new commands
4. Update database schema with new migration

### Integration Points

- **External Payment Gateways**: Replace simulated payment logic
- **Shipping Providers**: Integrate with real shipping APIs
- **Notification Services**: Add email/SMS notifications
- **Monitoring Tools**: Integrate with Application Insights or other APM tools

## 📚 Additional Resources

- [Saga Pattern Documentation](https://microservices.io/patterns/data/saga.html)
- [Azure Service Bus Documentation](https://docs.microsoft.com/en-us/azure/service-bus-messaging/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [.NET Background Services](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)

## 🤝 Contributing

This is a demonstration project showing production-level patterns and practices. Feel free to use this code as a starting point for your own saga implementations.

## 📄 License

This project is provided as-is for educational and demonstration purposes.
