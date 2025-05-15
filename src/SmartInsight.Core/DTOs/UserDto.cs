using SmartInsight.Core.Enums;
using System.Text.Json.Serialization;

namespace SmartInsight.Core.DTOs;

/// <summary>
/// DTO for user information
/// </summary>
public record UserDto
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; init; } = string.Empty;
    
    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; init; } = string.Empty;
    
    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; init; } = string.Empty;
    
    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; init; } = string.Empty;
    
    /// <summary>
    /// User's role in the system
    /// </summary>
    public UserRole Role { get; init; } = UserRole.User;
    
    /// <summary>
    /// Tenant ID the user belongs to
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the user is currently active
    /// </summary>
    public bool IsActive { get; init; } = true;
    
    /// <summary>
    /// Whether the email has been verified
    /// </summary>
    public bool EmailVerified { get; init; } = false;
    
    /// <summary>
    /// Whether the user account is locked
    /// </summary>
    public bool IsLocked { get; init; } = false;
    
    /// <summary>
    /// When the lockout expires (if applicable)
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; init; }
    
    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTimeOffset? LastLogin { get; init; }
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
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