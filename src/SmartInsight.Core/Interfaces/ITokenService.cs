using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartInsight.Core.Interfaces
{
    /// <summary>
    /// Interface for token generation, validation, and management
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generate a JWT token for a user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <param name="roles">User roles for claims</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <returns>The generated JWT token string</returns>
        Task<string> GenerateTokenAsync(string userId, IEnumerable<string> roles, string tenantId);
        
        /// <summary>
        /// Validate a JWT token and return claims
        /// </summary>
        /// <param name="token">The JWT token to validate</param>
        /// <returns>True if the token is valid, false otherwise</returns>
        Task<bool> ValidateTokenAsync(string token);
        
        /// <summary>
        /// Revoke a JWT token
        /// </summary>
        /// <param name="token">The JWT token to revoke</param>
        /// <returns>True if successfully revoked, false otherwise</returns>
        Task<bool> RevokeTokenAsync(string token);
        
        /// <summary>
        /// Extract claims from a JWT token
        /// </summary>
        /// <param name="token">The JWT token</param>
        /// <returns>Dictionary of claim type and values</returns>
        Task<Dictionary<string, string>> ExtractClaimsAsync(string token);
        
        /// <summary>
        /// Refresh a JWT token
        /// </summary>
        /// <param name="token">The expired token</param>
        /// <param name="refreshToken">The refresh token</param>
        /// <returns>New JWT token</returns>
        Task<string> RefreshTokenAsync(string token, string refreshToken);
        
        /// <summary>
        /// Generate a refresh token for a user
        /// </summary>
        /// <param name="userId">The user identifier</param>
        /// <returns>Refresh token string and expiration date</returns>
        Task<(string Token, DateTime Expiration)> GenerateRefreshTokenAsync(string userId);
    }
} 