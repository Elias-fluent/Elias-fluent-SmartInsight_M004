using System;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Interface for accessing tenant context information
/// </summary>
public interface ITenantAccessor
{
    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    /// <returns>The current tenant ID or null if not available</returns>
    Guid? GetCurrentTenantId();
    
    /// <summary>
    /// Gets the current RLS identifier for the tenant
    /// </summary>
    /// <returns>The RLS identifier or null if not available</returns>
    string? GetCurrentRlsIdentifier();
    
    /// <summary>
    /// Determines if the current context is in multi-tenant mode
    /// </summary>
    /// <returns>True if in a multi-tenant context, false otherwise</returns>
    bool IsMultiTenantContext();
    
    /// <summary>
    /// Checks if the current user can access all tenants
    /// </summary>
    /// <returns>True if the user can access all tenants, false otherwise</returns>
    bool CanAccessAllTenants();
} 