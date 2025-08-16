using Microsoft.Extensions.Logging;
using Moq;
using SagaPatternDemo.Host.Infrastructure.Data;
using SagaPatternDemo.Host.Shared.Models;

namespace SagaPatternDemo.Tests;

public class OrderSagaServiceTests
{
    [Fact]
    public async Task GetOrCreateSagaAsync_CreatesNewSaga_WhenNotExists()
    {
        // Arrange
        var sagaRepository = new Mock<ISagaRepository>();
        var logger = new Mock<ILogger<OrderSagaService>>();
        
        sagaRepository.Setup(r => r.GetByOrderIdAsync("ORDER-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderSaga?)null);
        
        var service = new OrderSagaService(sagaRepository.Object, logger.Object);

        // Act
        var result = await service.GetOrCreateSagaAsync("ORDER-001", 150.75m);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ORDER-001", result.OrderId);
        Assert.Equal(150.75m, result.Amount);
        Assert.Equal(SagaState.Created, result.State);
        
        sagaRepository.Verify(r => r.SaveAsync(It.IsAny<OrderSaga>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateSagaAsync_ReturnsExistingSaga_WhenExists()
    {
        // Arrange
        var existingSaga = new OrderSaga
        {
            OrderId = "ORDER-002",
            Amount = 200.00m,
            State = SagaState.PaymentSucceeded
        };

        var sagaRepository = new Mock<ISagaRepository>();
        var logger = new Mock<ILogger<OrderSagaService>>();
        
        sagaRepository.Setup(r => r.GetByOrderIdAsync("ORDER-002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSaga);
        
        var service = new OrderSagaService(sagaRepository.Object, logger.Object);

        // Act
        var result = await service.GetOrCreateSagaAsync("ORDER-002");

        // Assert
        Assert.Same(existingSaga, result);
        sagaRepository.Verify(r => r.SaveAsync(It.IsAny<OrderSaga>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_UpdatesPaymentState_WhenNotProcessed()
    {
        // Arrange
        var saga = new OrderSaga
        {
            OrderId = "ORDER-003",
            Amount = 100.00m,
            State = SagaState.Created,
            PaymentProcessed = false
        };

        var sagaRepository = new Mock<ISagaRepository>();
        var logger = new Mock<ILogger<OrderSagaService>>();
        
        sagaRepository.Setup(r => r.GetByOrderIdAsync("ORDER-003", It.IsAny<CancellationToken>()))
            .ReturnsAsync(saga);
        
        var service = new OrderSagaService(sagaRepository.Object, logger.Object);

        // Act
        var result = await service.ProcessPaymentAsync("ORDER-003", succeeded: true);

        // Assert
        Assert.True(result);
        Assert.True(saga.PaymentProcessed);
        Assert.Equal(SagaState.PaymentSucceeded, saga.State);
        
        sagaRepository.Verify(r => r.SaveAsync(saga, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ReturnsfalseAndDoesNotUpdate_WhenAlreadyProcessed()
    {
        // Arrange
        var saga = new OrderSaga
        {
            OrderId = "ORDER-004",
            Amount = 100.00m,
            State = SagaState.PaymentSucceeded,
            PaymentProcessed = true
        };

        var sagaRepository = new Mock<ISagaRepository>();
        var logger = new Mock<ILogger<OrderSagaService>>();
        
        sagaRepository.Setup(r => r.GetByOrderIdAsync("ORDER-004", It.IsAny<CancellationToken>()))
            .ReturnsAsync(saga);
        
        var service = new OrderSagaService(sagaRepository.Object, logger.Object);

        // Act
        var result = await service.ProcessPaymentAsync("ORDER-004", succeeded: true);

        // Assert
        Assert.False(result);
        sagaRepository.Verify(r => r.SaveAsync(It.IsAny<OrderSaga>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessShippingAsync_CompletesShipping_WhenPaymentSucceeded()
    {
        // Arrange
        var saga = new OrderSaga
        {
            OrderId = "ORDER-005",
            Amount = 100.00m,
            State = SagaState.PaymentSucceeded,
            PaymentProcessed = true,
            ShippingProcessed = false
        };

        var sagaRepository = new Mock<ISagaRepository>();
        var logger = new Mock<ILogger<OrderSagaService>>();
        
        sagaRepository.Setup(r => r.GetByOrderIdAsync("ORDER-005", It.IsAny<CancellationToken>()))
            .ReturnsAsync(saga);
        
        var service = new OrderSagaService(sagaRepository.Object, logger.Object);

        // Act
        var result = await service.ProcessShippingAsync("ORDER-005");

        // Assert
        Assert.True(result);
        Assert.True(saga.ShippingProcessed);
        Assert.Equal(SagaState.Completed, saga.State);
        
        sagaRepository.Verify(r => r.SaveAsync(saga, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CanProcessSaga_ReturnsTrue_ForProcessableStates()
    {
        // Arrange
        var sagaRepository = new Mock<ISagaRepository>();
        var logger = new Mock<ILogger<OrderSagaService>>();
        var service = new OrderSagaService(sagaRepository.Object, logger.Object);

        var processableSaga = new OrderSaga { State = SagaState.Created };
        var paymentSucceededSaga = new OrderSaga { State = SagaState.PaymentSucceeded };

        // Act & Assert
        Assert.True(service.CanProcessSaga(processableSaga));
        Assert.True(service.CanProcessSaga(paymentSucceededSaga));
    }

    [Fact]
    public async Task CanProcessSaga_ReturnsFalse_ForNonProcessableStates()
    {
        // Arrange
        var sagaRepository = new Mock<ISagaRepository>();
        var logger = new Mock<ILogger<OrderSagaService>>();
        var service = new OrderSagaService(sagaRepository.Object, logger.Object);

        var completedSaga = new OrderSaga { State = SagaState.Completed };
        var failedSaga = new OrderSaga { State = SagaState.Failed };
        var cancelledSaga = new OrderSaga { State = SagaState.Cancelled };

        // Act & Assert
        Assert.False(service.CanProcessSaga(completedSaga));
        Assert.False(service.CanProcessSaga(failedSaga));
        Assert.False(service.CanProcessSaga(cancelledSaga));
        Assert.False(service.CanProcessSaga(null!));
    }
}