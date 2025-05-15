using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Interfaces;
using SmartInsight.Data.Contexts;
using System.Collections;

namespace SmartInsight.Data.Repositories;

/// <summary>
/// Implementation of the Unit of Work pattern for coordinating multiple repositories
/// and managing transactions
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _transaction;
    private readonly Hashtable _repositories;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the UnitOfWork class
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger</param>
    public UnitOfWork(ApplicationDbContext dbContext, ILogger<UnitOfWork> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repositories = new Hashtable();
        _disposed = false;
    }

    /// <inheritdoc />
    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T).Name;

        if (!_repositories.ContainsKey(type))
        {
            var repositoryType = typeof(Repository<>);
            var repositoryInstance = Activator.CreateInstance(
                repositoryType.MakeGenericType(typeof(T)), _dbContext);

            if (repositoryInstance == null)
            {
                throw new InvalidOperationException($"Could not create repository instance for {type}");
            }

            _repositories.Add(type, repositoryInstance);
            _logger.LogDebug("Created repository for {EntityType}", type);
        }

        return (IRepository<T>)_repositories[type]!;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync()
    {
        try
        {
            var result = await _dbContext.SaveChangesAsync();
            _logger.LogDebug("Saved {Changes} changes to the database", result);
            return result;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error saving changes to the database");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
        {
            _logger.LogWarning("A transaction is already in progress");
            return;
        }

        _transaction = await _dbContext.Database.BeginTransactionAsync();
        _logger.LogDebug("Started a new database transaction");
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            _logger.LogWarning("No transaction to commit");
            return;
        }

        try
        {
            await _dbContext.SaveChangesAsync();
            await _transaction.CommitAsync();
            _logger.LogDebug("Committed transaction");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
        {
            _logger.LogWarning("No transaction to roll back");
            return;
        }

        try
        {
            await _transaction.RollbackAsync();
            _logger.LogDebug("Rolled back transaction");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources used by the UnitOfWork
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _dbContext.Dispose();
            _disposed = true;
            _logger.LogDebug("Disposed UnitOfWork");
        }
    }
} 