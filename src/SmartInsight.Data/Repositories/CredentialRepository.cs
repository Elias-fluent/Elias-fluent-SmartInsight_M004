using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Interfaces;
using SmartInsight.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartInsight.Data.Repositories;

/// <summary>
/// Repository for managing credentials
/// </summary>
public class CredentialRepository : MultiTenantRepository<Credential>, IRepository<Credential>
{
    private readonly ILogger<CredentialRepository> _logger;

    /// <summary>
    /// Creates a new credential repository
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="tenantAccessor">Tenant accessor</param>
    /// <param name="logger">Logger</param>
    public CredentialRepository(
        ApplicationDbContext dbContext,
        ITenantAccessor tenantAccessor,
        ILogger<CredentialRepository> logger)
        : base(dbContext, tenantAccessor, logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Gets a credential by its key
    /// </summary>
    /// <param name="key">Credential key</param>
    /// <returns>Credential entity or null if not found</returns>
    public async Task<Credential?> GetByKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));
        
        return await _dbSet
            .Where(c => c.Key == key)
            .FirstOrDefaultAsync();
    }
    
    /// <summary>
    /// Gets all credentials matching filters
    /// </summary>
    /// <param name="source">Optional source filter</param>
    /// <param name="group">Optional group filter</param>
    /// <returns>Collection of credentials</returns>
    public async Task<IReadOnlyList<Credential>> GetCredentialsAsync(string? source = null, string? group = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(c => c.Source == source);
            
        if (!string.IsNullOrWhiteSpace(group))
            query = query.Where(c => c.Group == group);
            
        return await query.ToListAsync();
    }
    
    /// <summary>
    /// Gets all credential keys matching filters
    /// </summary>
    /// <param name="source">Optional source filter</param>
    /// <param name="group">Optional group filter</param>
    /// <returns>Collection of credential keys</returns>
    public async Task<IReadOnlyList<string>> GetCredentialKeysAsync(string? source = null, string? group = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(c => c.Source == source);
            
        if (!string.IsNullOrWhiteSpace(group))
            query = query.Where(c => c.Group == group);
            
        return await query.Select(c => c.Key).ToListAsync();
    }
    
    /// <summary>
    /// Checks if a credential with the given key exists
    /// </summary>
    /// <param name="key">Credential key</param>
    /// <returns>True if the credential exists</returns>
    public async Task<bool> ExistsByKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));
        
        return await _dbSet.AnyAsync(c => c.Key == key);
    }
    
    /// <summary>
    /// Updates the last accessed time for a credential
    /// </summary>
    /// <param name="id">Credential ID</param>
    /// <returns>Success indicator</returns>
    public async Task<bool> UpdateLastAccessedAsync(Guid id)
    {
        var credential = await _dbSet.FindAsync(id);
        if (credential == null)
            return false;
            
        credential.LastAccessedAt = DateTime.UtcNow;
        credential.AccessCount++;
        
        await _dbContext.SaveChangesAsync();
        return true;
    }
    
    /// <summary>
    /// Purges expired credentials
    /// </summary>
    /// <returns>Number of purged credentials</returns>
    public async Task<int> PurgeExpiredCredentialsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredCredentials = await _dbSet
            .Where(c => c.ExpiresAt != null && c.ExpiresAt < now)
            .ToListAsync();
            
        if (expiredCredentials.Count == 0)
            return 0;
            
        _dbSet.RemoveRange(expiredCredentials);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Purged {Count} expired credentials", expiredCredentials.Count);
        return expiredCredentials.Count;
    }
} 