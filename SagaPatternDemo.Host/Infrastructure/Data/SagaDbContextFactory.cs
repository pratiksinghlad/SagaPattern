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
        
        // Use LocalDB connection string for migrations
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SagaPatternDemo;Trusted_Connection=true;MultipleActiveResultSets=true");

        return new SagaDbContext(optionsBuilder.Options);
    }
}