using Microsoft.EntityFrameworkCore;
using SagaPatternDemo.Host.Shared.Models;

namespace SagaPatternDemo.Host.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for saga persistence
/// </summary>
public class SagaDbContext : DbContext
{
    public SagaDbContext(DbContextOptions<SagaDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Order sagas table
    /// </summary>
    public DbSet<OrderSaga> OrderSagas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OrderSaga>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.State).HasConversion<string>();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            // Index for better performance on lookups
            entity.HasIndex(e => e.State);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}