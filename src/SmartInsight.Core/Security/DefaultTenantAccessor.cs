using SmartInsight.Core.Interfaces;
using System;

namespace SmartInsight.Core.Security;

/// <summary>
/// Simple implementation of ITenantAccessor that can be used in non-HTTP contexts
/// </summary>
public class DefaultTenantAccessor : ITenantAccessor
{
    private Guid? _currentTenantId;
    private bool _canAccessAllTenants;

    /// <summary>
    /// Creates a new instance of DefaultTenantAccessor
    /// </summary>
    /// <param name="tenantId">Optional tenant ID to use</param>
    /// <param name="canAccessAllTenants">Whether the current context can access all tenants</param>
    public DefaultTenantAccessor(Guid? tenantId = null, bool canAccessAllTenants = false)
    {
        _currentTenantId = tenantId;
        _canAccessAllTenants = canAccessAllTenants;
    }

    /// <summary>
    /// Sets the current tenant ID
    /// </summary>
    /// <param name="tenantId">The tenant ID to set</param>
    public void SetTenantId(Guid? tenantId)
    {
        _currentTenantId = tenantId;
    }

    /// <summary>
    /// Sets whether the current context can access all tenants
    /// </summary>
    /// <param name="canAccess">Whether the current context can access all tenants</param>
    public void SetCanAccessAllTenants(bool canAccess)
    {
        _canAccessAllTenants = canAccess;
    }

    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    /// <returns>The current tenant ID or null if not available</returns>
    public Guid? GetCurrentTenantId()
    {
        return _currentTenantId;
    }

    /// <summary>
    /// Gets the current RLS identifier for the tenant
    /// </summary>
    /// <returns>The RLS identifier or null if not available</returns>
    public string? GetCurrentRlsIdentifier()
    {
        return _currentTenantId?.ToString();
    }

    /// <summary>
    /// Determines if the current context is in multi-tenant mode
    /// </summary>
    /// <returns>True if in a multi-tenant context, false otherwise</returns>
    public bool IsMultiTenantContext()
    {
        return _currentTenantId.HasValue;
    }

    /// <summary>
    /// Checks if the current user can access all tenants
    /// </summary>
    /// <returns>True if the user can access all tenants, false otherwise</returns>
    public bool CanAccessAllTenants()
    {
        return _canAccessAllTenants;
    }
} 