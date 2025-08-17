using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SagaPatternDemo.Host.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating DbContext during migrations
/// </summary>
public class SagaDbContextFactory : IDesignTimeDbContextFactory<SagaDbContext>
{
    public SagaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SagaDbContext>();
        
        // Use SQLCipher with default password for migrations
        var connectionString = SqlCipherExtensions.BuildSqlCipherConnectionString(
            dataSource: "SagaPatternDemo.db",
            password: "your-strong-password-here");
            
        optionsBuilder.UseSqlCipher(connectionString);

        return new SagaDbContext(optionsBuilder.Options);
    }
}