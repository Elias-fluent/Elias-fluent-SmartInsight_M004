using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartInsight.API.Controllers;

/// <summary>
/// Base class for all API controllers providing common functionality
/// </summary>
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Gets the current user's tenant ID from claims
    /// </summary>
    /// <returns>Tenant ID or null if not found</returns>
    protected Guid? GetCurrentTenantId()
    {
        var tenantClaim = User.FindFirst(Core.Security.SmartInsightClaimTypes.TenantId);
        if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets the current user's ID from claims
    /// </summary>
    /// <returns>User ID or null if not found</returns>
    protected string? GetCurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }
    
    /// <summary>
    /// Checks if the current user has access to all tenants (admin)
    /// </summary>
    /// <returns>True if user can access all tenants</returns>
    protected bool CanAccessAllTenants()
    {
        var claim = User.FindFirst(Core.Security.SmartInsightClaimTypes.CanAccessAllTenants);
        return claim != null && bool.TryParse(claim.Value, out var canAccess) && canAccess;
    }
} 