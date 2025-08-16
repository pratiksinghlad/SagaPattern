using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SagaPatternDemo.Host.Shared.Models;

namespace SagaPatternDemo.Host.Infrastructure.Data;

/// <summary>
/// Repository interface for saga operations
/// </summary>
public interface ISagaRepository
{
    /// <summary>
    /// Gets an order saga by its ID
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The order saga or null if not found</returns>
    Task<OrderSaga?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates an order saga
    /// </summary>
    /// <param name="saga">The saga to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task SaveAsync(OrderSaga saga, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sagas with a specific state
    /// </summary>
    /// <param name="state">The saga state to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of sagas with the specified state</returns>
    Task<List<OrderSaga>> GetByStateAsync(SagaState state, CancellationToken cancellationToken = default);
}

/// <summary>
/// Entity Framework implementation of saga repository
/// </summary>
public class SagaRepository : ISagaRepository
{
    private readonly SagaDbContext _context;
    private readonly ILogger<SagaRepository> _logger;

    public SagaRepository(SagaDbContext context, ILogger<SagaRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OrderSaga?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Order ID cannot be null or empty", nameof(orderId));

        try
        {
            return await _context.OrderSagas
                .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saga for OrderId: {OrderId}", orderId);
            throw;
        }
    }

    public async Task SaveAsync(OrderSaga saga, CancellationToken cancellationToken = default)
    {
        if (saga == null)
            throw new ArgumentNullException(nameof(saga));

        try
        {
            saga.UpdatedAt = DateTime.UtcNow;

            var existingSaga = await _context.OrderSagas
                .FirstOrDefaultAsync(s => s.OrderId == saga.OrderId, cancellationToken);

            if (existingSaga == null)
            {
                _context.OrderSagas.Add(saga);
                _logger.LogInformation("Creating new saga for OrderId: {OrderId}", saga.OrderId);
            }
            else
            {
                _context.Entry(existingSaga).CurrentValues.SetValues(saga);
                _logger.LogInformation("Updating existing saga for OrderId: {OrderId}", saga.OrderId);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving saga for OrderId: {OrderId}", saga.OrderId);
            throw;
        }
    }

    public async Task<List<OrderSaga>> GetByStateAsync(SagaState state, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OrderSagas
                .Where(s => s.State == state)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sagas by state: {State}", state);
            throw;
        }
    }
}