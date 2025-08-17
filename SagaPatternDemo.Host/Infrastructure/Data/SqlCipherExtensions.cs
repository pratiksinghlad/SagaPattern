using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace SagaPatternDemo.Host.Infrastructure.Data;

/// <summary>
/// Extension methods for SQLCipher configuration and setup
/// </summary>
public static class SqlCipherExtensions
{
    /// <summary>
    /// Initializes SQLCipher bundle. Should be called once at application startup.
    /// </summary>
    public static void InitializeSqlCipher()
    {
        // Initialize SQLCipher bundle for encryption support
        Batteries_V2.Init();
    }

    /// <summary>
    /// Builds a SQLCipher connection string with password encryption
    /// </summary>
    /// <param name="dataSource">Database file path</param>
    /// <param name="password">Encryption password</param>
    /// <returns>Formatted SQLCipher connection string</returns>
    public static string BuildSqlCipherConnectionString(string dataSource, string password)
    {
        if (string.IsNullOrEmpty(dataSource))
            throw new ArgumentException("Data source cannot be null or empty", nameof(dataSource));
        
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        return $"Data Source={dataSource};Password={password}";
    }

    /// <summary>
    /// Configures SQLite with SQLCipher encryption for Entity Framework
    /// </summary>
    /// <param name="optionsBuilder">DbContext options builder</param>
    /// <param name="connectionString">SQLCipher connection string with password</param>
    /// <returns>Configured options builder</returns>
    public static DbContextOptionsBuilder UseSqlCipher(
        this DbContextOptionsBuilder optionsBuilder, 
        string connectionString)
    {
        InitializeSqlCipher();
        return optionsBuilder.UseSqlite(connectionString);
    }
}
