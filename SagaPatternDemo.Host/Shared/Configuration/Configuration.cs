namespace SagaPatternDemo.Host.Shared.Configuration;

/// <summary>
/// Configuration settings for Azure Service Bus using ConnectionStrings
/// </summary>
public class AzureServiceBusConfiguration
{
    public const string SectionName = "ConnectionStrings:Asb";

    /// <summary>
    /// Service Bus connection URL with endpoint and authentication
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Shared Access Key for Service Bus authentication
    /// </summary>
    public string SharedAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Prefix for queue/topic names
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full connection string by combining URL and SharedAccessKey
    /// </summary>
    public string ConnectionString => $"{Url}{SharedAccessKey}";

    /// <summary>
    /// Queue/topic name for order events (with prefix)
    /// </summary>
    public string OrdersQueue => $"orders"; //{Prefix}-

    /// <summary>
    /// Queue/topic name for payment events (with prefix)
    /// </summary>
    public string PaymentsQueue => $"payments";

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
    /// Connection string for the SQLite database
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database login ID (for reference/audit purposes)
    /// </summary>
    public string LoginId { get; set; } = string.Empty;

    /// <summary>
    /// Database password for SQLCipher encryption
    /// </summary>
    public string Password { get; set; } = string.Empty;
}