using Microsoft.EntityFrameworkCore;
using SagaPatternDemo.Host.Infrastructure.Data;
using SagaPatternDemo.Host.Shared.Configuration;

// Test SQLite/SQLCipher setup
//Console.WriteLine("Testing SQLite with SQLCipher...");

var databaseConfig = new DatabaseConfiguration
{
    ConnectionString = "Data Source=TestSaga.db",
    Password = "test-password-123"
};

var connectionString = SqlCipherExtensions.BuildSqlCipherConnectionString(
    dataSource: "TestSaga.db",
    password: databaseConfig.Password);

Console.WriteLine($"Connection String: {connectionString}");

var optionsBuilder = new DbContextOptionsBuilder<SagaDbContext>();
optionsBuilder.UseSqlCipher(connectionString);

using var context = new SagaDbContext(optionsBuilder.Options);

try
{
    await context.Database.EnsureCreatedAsync();
    Console.WriteLine("✅ Database created successfully!");

    // Check if file exists
    if (File.Exists("TestSaga.db"))
    {
        Console.WriteLine("✅ Database file exists on disk");
        var fileInfo = new FileInfo("TestSaga.db");
        Console.WriteLine($"   File size: {fileInfo.Length} bytes");
    }
    else
    {
        Console.WriteLine("❌ Database file not found on disk");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database creation failed: {ex.Message}");
}
