using Microsoft.EntityFrameworkCore;
using SmartInsight.Core.Interfaces;
using SmartInsight.Data.Contexts;
using System.Linq.Expressions;

namespace SmartInsight.Data.Repositories;

/// <summary>
/// Generic repository implementation using Entity Framework Core
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    /// <summary>
    /// Database context
    /// </summary>
    protected readonly ApplicationDbContext _dbContext;
    
    /// <summary>
    /// DbSet for the entity type
    /// </summary>
    protected readonly DbSet<T> _dbSet;

    /// <summary>
    /// Initializes a new instance of the Repository class
    /// </summary>
    /// <param name="dbContext">Database context</param>
    public Repository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbSet = _dbContext.Set<T>();
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(params string[] includeProperties)
    {
        IQueryable<T> query = _dbSet;
        
        query = ApplyIncludes(query, includeProperties);
        
        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, params string[] includeProperties)
    {
        IQueryable<T> query = _dbSet;
        
        query = query.Where(predicate);
        query = ApplyIncludes(query, includeProperties);
        
        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params string[] includeProperties)
    {
        IQueryable<T> query = _dbSet;
        
        query = query.Where(predicate);
        query = ApplyIncludes(query, includeProperties);
        
        return await query.FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public virtual async Task<T> FirstAsync(Expression<Func<T, bool>> predicate, params string[] includeProperties)
    {
        IQueryable<T> query = _dbSet;
        
        query = query.Where(predicate);
        query = ApplyIncludes(query, includeProperties);
        
        return await query.FirstAsync();
    }

    /// <inheritdoc />
    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate == null 
            ? await _dbSet.CountAsync() 
            : await _dbSet.CountAsync(predicate);
    }

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        await _dbSet.AddRangeAsync(entityList);
        return entityList;
    }

    /// <inheritdoc />
    public virtual Task<T> UpdateAsync(T entity)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public virtual Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        foreach (var entity in entityList)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
        }
        return Task.FromResult<IEnumerable<T>>(entityList);
    }

    /// <inheritdoc />
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
        {
            return false;
        }
        
        return await DeleteAsync(entity);
    }

    /// <inheritdoc />
    public virtual Task<bool> DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public virtual Task<bool> DeleteRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public virtual IQueryable<T> AsQueryable(bool tracking = false)
    {
        return tracking
            ? _dbSet
            : _dbSet.AsNoTracking();
    }

    /// <summary>
    /// Applies the specified include properties to the query
    /// </summary>
    /// <param name="query">The query to apply includes to</param>
    /// <param name="includeProperties">The properties to include</param>
    /// <returns>The query with includes applied</returns>
    private static IQueryable<T> ApplyIncludes(IQueryable<T> query, params string[] includeProperties)
    {
        return includeProperties.Aggregate(query, 
            (current, include) => current.Include(include));
    }
} 