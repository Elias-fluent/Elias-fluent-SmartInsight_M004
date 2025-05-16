using System;
using System.Collections.Generic;
using SmartInsight.Core.Enums;
using System.Text.Json.Serialization;

namespace SmartInsight.Core.DTOs;

/// <summary>
/// Data transfer object for user information
/// </summary>
public class UserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Username
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name or full name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the account is active
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; set; }
    
    /// <summary>
    /// User roles
    /// </summary>
    public List<string> Roles { get; set; } = new List<string>();
}

/// <summary>
/// DTO for user registration
/// </summary>
public record RegisterUserDto
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    public required string Username { get; init; }
    
    /// <summary>
    /// Email address
    /// </summary>
    public required string Email { get; init; }
    
    /// <summary>
    /// Password (plain text for registration only)
    /// </summary>
    public required string Password { get; init; }
    
    /// <summary>
    /// First name
    /// </summary>
    public required string FirstName { get; init; }
    
    /// <summary>
    /// Last name
    /// </summary>
    public required string LastName { get; init; }
    
    /// <summary>
    /// User's role in the system
    /// </summary>
    public UserRole Role { get; init; } = UserRole.User;
    
    /// <summary>
    /// Tenant ID the user belongs to
    /// </summary>
    public required string TenantId { get; init; }
}

/// <summary>
/// DTO for user login
/// </summary>
public record LoginDto
{
    /// <summary>
    /// Username or email for login
    /// </summary>
    public required string UsernameOrEmail { get; init; }
    
    /// <summary>
    /// Password (plain text for login only)
    /// </summary>
    public required string Password { get; init; }
    
    /// <summary>
    /// Whether to remember the user for extended session
    /// </summary>
    public bool RememberMe { get; init; } = false;
}

/// <summary>
/// DTO for authentication response
/// </summary>
public record AuthResponseDto
{
    /// <summary>
    /// Authentication token (JWT)
    /// </summary>
    public string Token { get; init; } = string.Empty;
    
    /// <summary>
    /// Refresh token for obtaining new JWT tokens
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
    
    /// <summary>
    /// User information
    /// </summary>
    public UserDto? User { get; init; }
    
    /// <summary>
    /// When the token expires
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; } = DateTimeOffset.UtcNow.AddHours(1);
} 