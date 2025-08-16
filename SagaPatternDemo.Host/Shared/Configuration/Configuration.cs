namespace SagaPatternDemo.Host.Shared.Configuration;

/// <summary>
/// Configuration settings for Azure Service Bus
/// </summary>
public class ServiceBusConfiguration
{
    public const string SectionName = "ServiceBus";

    /// <summary>
    /// Service Bus namespace (e.g., your-namespace.servicebus.windows.net)
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Azure tenant ID for Service Principal authentication
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure client ID for Service Principal authentication
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure client secret for Service Principal authentication
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Queue/topic name for order events
    /// </summary>
    public string OrdersQueue { get; set; } = "orders";

    /// <summary>
    /// Queue/topic name for payment events
    /// </summary>
    public string PaymentsQueue { get; set; } = "payments";

    /// <summary>
    /// Maximum number of concurrent calls for message processing
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 1;
}

/// <summary>
/// Configuration settings for database connection
/// </summary>
public class DatabaseConfiguration
{
    public const string SectionName = "Database";

    /// <summary>
    /// Connection string for the database
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}