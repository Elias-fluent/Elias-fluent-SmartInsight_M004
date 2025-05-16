using SmartInsight.Core.DTOs;
using SmartInsight.Core.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartInsight.API.Security;

/// <summary>
/// Interface for JWT token service
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the specified user
    /// </summary>
    /// <param name="user">User to generate token for</param>
    /// <param name="roles">User's roles</param>
    /// <param name="tenantId">User's tenant ID</param>
    /// <returns>Token details including access and refresh tokens</returns>
    Task<TokenDto> GenerateTokenAsync(ApplicationUser user, IList<string> roles, Guid? tenantId);
    
    /// <summary>
    /// Validates the provided JWT token
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateToken(string token);
    
    /// <summary>
    /// Gets claims from a JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Claims from the token</returns>
    IEnumerable<Claim> GetClaimsFromToken(string token);
} 