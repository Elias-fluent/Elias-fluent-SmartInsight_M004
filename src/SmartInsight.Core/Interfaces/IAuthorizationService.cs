using System.Threading.Tasks;

namespace SmartInsight.Core.Interfaces
{
    /// <summary>
    /// Interface for authorization service that handles permission checks
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Checks if a user is authorized to perform an action on a resource
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="action">The action being performed</param>
        /// <param name="resourceType">Type of resource being accessed</param>
        /// <param name="resourceId">Optional resource identifier</param>
        /// <returns>True if the user is authorized, false otherwise</returns>
        Task<bool> IsAuthorizedAsync(string userId, string action, string resourceType, string? resourceId = null);
        
        /// <summary>
        /// Checks if a user belongs to a specific role
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="role">Role name to check</param>
        /// <returns>True if the user has the role, false otherwise</returns>
        Task<bool> IsInRoleAsync(string userId, string role);
        
        /// <summary>
        /// Validates that a user belongs to a specified tenant
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="tenantId">Tenant identifier</param>
        /// <returns>True if the user belongs to the tenant, false otherwise</returns>
        Task<bool> IsInTenantAsync(string userId, string tenantId);
    }
} 