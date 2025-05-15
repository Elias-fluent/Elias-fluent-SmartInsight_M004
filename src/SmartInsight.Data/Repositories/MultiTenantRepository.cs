using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Interfaces;
using SmartInsight.Data.Contexts;
using System.Linq.Expressions;

namespace SmartInsight.Data.Repositories;

/// <summary>
/// Repository implementation for multi-tenant entities
/// that enforces tenant isolation
/// </summary>
/// <typeparam name="T">Entity type that extends BaseMultiTenantEntity</typeparam>
public class MultiTenantRepository<T> : Repository<T>, IRepository<T> where T : BaseMultiTenantEntity
{
    private readonly ITenantAccessor _tenantAccessor;
    private readonly ILogger<MultiTenantRepository<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the MultiTenantRepository class
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="tenantAccessor">Tenant accessor for retrieving the current tenant ID</param>
    /// <param name="logger">Logger</param>
    public MultiTenantRepository(
        ApplicationDbContext dbContext, 
        ITenantAccessor tenantAccessor,
        ILogger<MultiTenantRepository<T>> logger) 
        : base(dbContext)
    {
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current tenant ID from the tenant accessor
    /// </summary>
    /// <returns>Current tenant ID</returns>
    /// <exception cref="InvalidOperationException">Thrown if tenant ID is empty</exception>
    protected Guid GetCurrentTenantId()
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        
        if (tenantId == null || tenantId == Guid.Empty)
        {
            _logger.LogError("Tenant ID is empty in the current context");
            throw new InvalidOperationException("Tenant ID is required for multi-tenant operations");
        }
        
        return tenantId.Value;
    }

    /// <summary>
    /// Creates a query filtered by the current tenant ID
    /// </summary>
    /// <returns>Query filtered by tenant ID</returns>
    protected IQueryable<T> GetTenantFilteredQuery()
    {
        var tenantId = GetCurrentTenantId();
        return base.AsQueryable().Where(e => e.TenantId == tenantId);
    }

    /// <inheritdoc />
    public override async Task<T?> GetByIdAsync(Guid id)
    {
        var tenantId = GetCurrentTenantId();
        return await _dbSet.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyList<T>> GetAllAsync(params string[] includeProperties)
    {
        IQueryable<T> query = GetTenantFilteredQuery();
        query = ApplyIncludes(query, includeProperties);
        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, params string[] includeProperties)
    {
        IQueryable<T> query = GetTenantFilteredQuery();
        query = query.Where(predicate);
        query = ApplyIncludes(query, includeProperties);
        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public override async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params string[] includeProperties)
    {
        IQueryable<T> query = GetTenantFilteredQuery();
        query = query.Where(predicate);
        query = ApplyIncludes(query, includeProperties);
        return await query.FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public override async Task<T> FirstAsync(Expression<Func<T, bool>> predicate, params string[] includeProperties)
    {
        IQueryable<T> query = GetTenantFilteredQuery();
        query = query.Where(predicate);
        query = ApplyIncludes(query, includeProperties);
        return await query.FirstAsync();
    }

    /// <inheritdoc />
    public override async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await GetTenantFilteredQuery().AnyAsync(predicate);
    }

    /// <inheritdoc />
    public override async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var query = GetTenantFilteredQuery();
        return predicate == null 
            ? await query.CountAsync() 
            : await query.CountAsync(predicate);
    }

    /// <inheritdoc />
    public override async Task<T> AddAsync(T entity)
    {
        var tenantId = GetCurrentTenantId();
        entity.TenantId = tenantId;
        
        _logger.LogDebug("Setting tenant ID {TenantId} for new {EntityType} entity", 
            tenantId, typeof(T).Name);
            
        return await base.AddAsync(entity);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        var tenantId = GetCurrentTenantId();
        var entityList = entities.ToList();
        
        foreach (var entity in entityList)
        {
            entity.TenantId = tenantId;
        }
        
        _logger.LogDebug("Setting tenant ID {TenantId} for {Count} new {EntityType} entities", 
            tenantId, entityList.Count, typeof(T).Name);
            
        return await base.AddRangeAsync(entityList);
    }

    /// <inheritdoc />
    public override async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
        {
            return false;
        }
        
        return await DeleteAsync(entity);
    }

    /// <inheritdoc />
    public override IQueryable<T> AsQueryable(bool tracking = false)
    {
        var query = GetTenantFilteredQuery();
        return tracking ? query : query.AsNoTracking();
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