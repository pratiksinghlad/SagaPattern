using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SagaPatternDemo.Host.Infrastructure.Data;
using SagaPatternDemo.Host.Shared.Models;

namespace SagaPatternDemo.Tests;

public class SagaRepositoryTests
{
    private SagaDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SagaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new SagaDbContext(options);
    }

    [Fact]
    public async Task GetByOrderIdAsync_ReturnsNull_WhenSagaNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<SagaRepository>>();
        var repository = new SagaRepository(context, logger.Object);

        // Act
        var result = await repository.GetByOrderIdAsync("non-existent-order");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_CreatesNewSaga_WhenSagaDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<SagaRepository>>();
        var repository = new SagaRepository(context, logger.Object);

        var saga = new OrderSaga
        {
            OrderId = "TEST-001",
            Amount = 100.50m,
            State = SagaState.Created
        };

        // Act
        await repository.SaveAsync(saga);

        // Assert
        var savedSaga = await repository.GetByOrderIdAsync("TEST-001");
        Assert.NotNull(savedSaga);
        Assert.Equal("TEST-001", savedSaga.OrderId);
        Assert.Equal(100.50m, savedSaga.Amount);
        Assert.Equal(SagaState.Created, savedSaga.State);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingSaga_WhenSagaExists()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<SagaRepository>>();
        var repository = new SagaRepository(context, logger.Object);

        var saga = new OrderSaga
        {
            OrderId = "TEST-002",
            Amount = 200.00m,
            State = SagaState.Created
        };

        await repository.SaveAsync(saga);

        // Act
        saga.State = SagaState.PaymentSucceeded;
        saga.PaymentProcessed = true;
        await repository.SaveAsync(saga);

        // Assert
        var updatedSaga = await repository.GetByOrderIdAsync("TEST-002");
        Assert.NotNull(updatedSaga);
        Assert.Equal(SagaState.PaymentSucceeded, updatedSaga.State);
        Assert.True(updatedSaga.PaymentProcessed);
    }

    [Fact]
    public async Task GetByStateAsync_ReturnsCorrectSagas()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<SagaRepository>>();
        var repository = new SagaRepository(context, logger.Object);

        var saga1 = new OrderSaga { OrderId = "TEST-003", Amount = 100m, State = SagaState.Created };
        var saga2 = new OrderSaga { OrderId = "TEST-004", Amount = 200m, State = SagaState.PaymentSucceeded };
        var saga3 = new OrderSaga { OrderId = "TEST-005", Amount = 300m, State = SagaState.Created };

        await repository.SaveAsync(saga1);
        await repository.SaveAsync(saga2);
        await repository.SaveAsync(saga3);

        // Act
        var createdSagas = await repository.GetByStateAsync(SagaState.Created);

        // Assert
        Assert.Equal(2, createdSagas.Count);
        Assert.All(createdSagas, s => Assert.Equal(SagaState.Created, s.State));
    }
}