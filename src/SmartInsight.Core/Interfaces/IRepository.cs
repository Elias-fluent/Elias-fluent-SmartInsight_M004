using System.Linq.Expressions;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Generic repository interface defining common CRUD operations for entity types
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all entities of type T
    /// </summary>
    /// <param name="includeProperties">Optional navigation properties to include</param>
    /// <returns>Collection of entities</returns>
    Task<IReadOnlyList<T>> GetAllAsync(params string[] includeProperties);

    /// <summary>
    /// Finds entities that match the specified criteria
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="includeProperties">Optional navigation properties to include</param>
    /// <returns>Collection of matching entities</returns>
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, params string[] includeProperties);

    /// <summary>
    /// Returns a single entity matching the specified criteria or default if none is found
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="includeProperties">Optional navigation properties to include</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params string[] includeProperties);

    /// <summary>
    /// Returns a single entity matching the specified criteria or throws an exception if none is found
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="includeProperties">Optional navigation properties to include</param>
    /// <returns>The entity</returns>
    Task<T> FirstAsync(Expression<Func<T, bool>> predicate, params string[] includeProperties);
    
    /// <summary>
    /// Determines whether any entity matches the specified criteria
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <returns>True if matching entities exist, false otherwise</returns>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Gets the count of entities matching the specified criteria
    /// </summary>
    /// <param name="predicate">Optional filter predicate</param>
    /// <returns>Count of matching entities</returns>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    
    /// <summary>
    /// Adds a new entity
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <returns>The added entity</returns>
    Task<T> AddAsync(T entity);
    
    /// <summary>
    /// Adds a collection of new entities
    /// </summary>
    /// <param name="entities">Entities to add</param>
    /// <returns>The added entities</returns>
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    
    /// <summary>
    /// Updates an existing entity
    /// </summary>
    /// <param name="entity">Entity to update</param>
    /// <returns>The updated entity</returns>
    Task<T> UpdateAsync(T entity);
    
    /// <summary>
    /// Updates a collection of existing entities
    /// </summary>
    /// <param name="entities">Entities to update</param>
    /// <returns>The updated entities</returns>
    Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);
    
    /// <summary>
    /// Deletes an entity by ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>True if the entity was deleted, false otherwise</returns>
    Task<bool> DeleteAsync(Guid id);
    
    /// <summary>
    /// Deletes an entity
    /// </summary>
    /// <param name="entity">Entity to delete</param>
    /// <returns>True if the entity was deleted, false otherwise</returns>
    Task<bool> DeleteAsync(T entity);
    
    /// <summary>
    /// Deletes a collection of entities
    /// </summary>
    /// <param name="entities">Entities to delete</param>
    /// <returns>True if the entities were deleted, false otherwise</returns>
    Task<bool> DeleteRangeAsync(IEnumerable<T> entities);
    
    /// <summary>
    /// Gets a queryable representation of the entity set
    /// </summary>
    /// <param name="tracking">Whether to enable change tracking</param>
    /// <returns>Queryable representation</returns>
    IQueryable<T> AsQueryable(bool tracking = false);
} 