namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Interface for the Unit of Work pattern that coordinates the work of multiple repositories
/// by providing a single transaction context
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets a repository for the specified entity type
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <returns>Repository for the entity type</returns>
    IRepository<T> Repository<T>() where T : class;
    
    /// <summary>
    /// Saves all changes made through the repositories to the database
    /// </summary>
    /// <returns>Number of state entries written to the database</returns>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Begins a new transaction
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task CommitTransactionAsync();

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RollbackTransactionAsync();
} 