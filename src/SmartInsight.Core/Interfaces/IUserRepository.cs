using SmartInsight.Core.Entities;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Repository interface for User entities
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets users by role name
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>List of users with the specified role</returns>
    Task<IReadOnlyList<User>> GetUsersByRoleAsync(string roleName);
    
    /// <summary>
    /// Searches for users by name or email
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="maxResults">Maximum number of results</param>
    /// <returns>List of matching users</returns>
    Task<IReadOnlyList<User>> SearchUsersAsync(string searchTerm, int maxResults = 20);
    
    /// <summary>
    /// Gets active users
    /// </summary>
    /// <returns>List of active users</returns>
    Task<IReadOnlyList<User>> GetActiveUsersAsync();
} 