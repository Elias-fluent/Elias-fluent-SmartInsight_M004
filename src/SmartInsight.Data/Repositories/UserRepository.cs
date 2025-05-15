using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Enums;
using SmartInsight.Core.Interfaces;
using SmartInsight.Data.Contexts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartInsight.Data.Repositories;

/// <summary>
/// Repository for User entities
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    private readonly ITenantAccessor _tenantAccessor;
    private readonly ILogger<UserRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the UserRepository class
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="tenantAccessor">Tenant accessor</param>
    /// <param name="logger">Logger</param>
    public UserRepository(
        ApplicationDbContext dbContext,
        ITenantAccessor tenantAccessor,
        ILogger<UserRepository> logger) 
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
    protected IQueryable<User> GetTenantFilteredQuery()
    {
        var tenantId = GetCurrentTenantId();
        return base.AsQueryable().Where(e => e.PrimaryTenantId == tenantId);
    }
    
    /// <inheritdoc />
    public override async Task<IReadOnlyList<User>> GetAllAsync(params string[] includeProperties)
    {
        IQueryable<User> query = GetTenantFilteredQuery();
        query = ApplyIncludes(query, includeProperties);
        return await query.ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetUsersByRoleAsync(string roleName)
    {
        var role = Enum.TryParse<UserRole>(roleName, true, out var userRole)
            ? userRole
            : UserRole.User;
            
        return await GetTenantFilteredQuery()
            .Where(u => u.Role == role)
            .ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> SearchUsersAsync(string searchTerm, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllAsync();
        }
        
        var normalizedSearch = searchTerm.ToLower().Trim();
        
        return await GetTenantFilteredQuery()
            .Where(u => 
                (u.FirstName != null && u.FirstName.ToLower().Contains(normalizedSearch)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(normalizedSearch)) ||
                u.Email.ToLower().Contains(normalizedSearch) ||
                u.Username.ToLower().Contains(normalizedSearch))
            .Take(maxResults)
            .ToListAsync();
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetActiveUsersAsync()
    {
        return await GetTenantFilteredQuery()
            .Where(u => u.IsActive)
            .ToListAsync();
    }
    
    /// <summary>
    /// Applies the specified include properties to the query
    /// </summary>
    /// <param name="query">The query to apply includes to</param>
    /// <param name="includeProperties">The properties to include</param>
    /// <returns>The query with includes applied</returns>
    private static IQueryable<User> ApplyIncludes(IQueryable<User> query, params string[] includeProperties)
    {
        return includeProperties.Aggregate(query, 
            (current, include) => current.Include(include));
    }
} 