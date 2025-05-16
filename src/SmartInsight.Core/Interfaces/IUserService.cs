using System.Collections.Generic;
using System.Threading.Tasks;
using SmartInsight.Core.Models;

namespace SmartInsight.Core.Interfaces
{
    /// <summary>
    /// Interface for user management operations
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Authenticate a user with username and password
        /// </summary>
        /// <param name="username">Username or email</param>
        /// <param name="password">Password</param>
        /// <returns>Authentication result with token if successful</returns>
        Task<AuthenticationResult> AuthenticateAsync(string username, string password);
        
        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="email">Email address</param>
        /// <param name="password">Password</param>
        /// <param name="tenantId">Tenant identifier</param>
        /// <returns>Registration result</returns>
        Task<RegistrationResult> RegisterAsync(string username, string email, string password, string tenantId);
        
        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>User data if found, null otherwise</returns>
        Task<UserData> GetUserAsync(string userId);
        
        /// <summary>
        /// Get user by username or email
        /// </summary>
        /// <param name="usernameOrEmail">Username or email</param>
        /// <returns>User data if found, null otherwise</returns>
        Task<UserData> GetUserByUsernameOrEmailAsync(string usernameOrEmail);
        
        /// <summary>
        /// Get users by tenant
        /// </summary>
        /// <param name="tenantId">Tenant identifier</param>
        /// <returns>List of users in the tenant</returns>
        Task<IEnumerable<UserData>> GetUsersByTenantAsync(string tenantId);
        
        /// <summary>
        /// Get user roles
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>List of roles for the user</returns>
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);
        
        /// <summary>
        /// Update user data
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="userData">Updated user data</param>
        /// <returns>True if update successful, false otherwise</returns>
        Task<bool> UpdateUserAsync(string userId, UserData userData);
        
        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="currentPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        /// <returns>True if password changed successfully, false otherwise</returns>
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
        
        /// <summary>
        /// Delete user
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteUserAsync(string userId);
    }
} 