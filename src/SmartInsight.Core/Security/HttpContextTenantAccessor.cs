/*
 * This implementation has been moved to the SmartInsight.API project
 * in the Security folder (HttpContextTenantAccessor.cs).
 * 
 * This is because the implementation requires ASP.NET Core HTTP dependencies
 * which should not be included in the Core project.
 * 
 * Use DefaultTenantAccessor instead for non-HTTP contexts.
 */

/*
 * NOTE: This file must be moved to a web project that has ASP.NET Core HTTP dependencies.
 * It is kept here as documentation of the implementation.
 * Use DefaultTenantAccessor instead for non-HTTP contexts.
 *
using Microsoft.AspNetCore.Http;
using SmartInsight.Core.Interfaces;
using System;
using System.Linq;
using System.Security.Claims;

namespace SmartInsight.Core.Security;

/// <summary>
/// Implementation of ITenantAccessor that retrieves tenant information from the HTTP context and claims
/// </summary>
public class HttpContextTenantAccessor : ITenantAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of HttpContextTenantAccessor
    /// </summary>
    /// <param name="httpContextAccessor">HTTP context accessor</param>
    public HttpContextTenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Gets the current tenant ID from the user's claims
    /// </summary>
    /// <returns>The current tenant ID or null if not available</returns>
    public Guid? GetCurrentTenantId()
    {
        var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(SmartInsightClaimTypes.TenantId);

        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out Guid tenantId))
        {
            return tenantId;
        }

        return null;
    }

    /// <summary>
    /// Gets the current RLS identifier for the tenant
    /// </summary>
    /// <returns>The RLS identifier or null if not available</returns>
    public string? GetCurrentRlsIdentifier()
    {
        // In a real implementation, this might look up the RLS identifier from a cache or database
        // For now, we'll just use the tenant ID as the RLS identifier
        var tenantId = GetCurrentTenantId();
        return tenantId?.ToString();
    }

    /// <summary>
    /// Determines if the current context is in multi-tenant mode
    /// </summary>
    /// <returns>True if in a multi-tenant context, false otherwise</returns>
    public bool IsMultiTenantContext()
    {
        return GetCurrentTenantId() != null;
    }

    /// <summary>
    /// Checks if the current user can access all tenants
    /// </summary>
    /// <returns>True if the user can access all tenants, false otherwise</returns>
    public bool CanAccessAllTenants()
    {
        var canAccessAllTenantsClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(SmartInsightClaimTypes.CanAccessAllTenants);
        
        if (canAccessAllTenantsClaim != null && bool.TryParse(canAccessAllTenantsClaim.Value, out bool canAccess))
        {
            return canAccess;
        }

        // Check if the user has the Admin role as a fallback
        return _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
    }
}
*/ 