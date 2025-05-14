namespace SmartInsight.Core.Constants;

/// <summary>
/// Constants for security and authentication
/// </summary>
public static class SecurityConstants
{
    /// <summary>
    /// Default password policy regular expression
    /// </summary>
    public const string PasswordPolicyRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
    
    /// <summary>
    /// Human-readable password policy description
    /// </summary>
    public const string PasswordPolicyDescription = "Password must be at least 8 characters and include at least one uppercase letter, one lowercase letter, one number, and one special character.";
    
    /// <summary>
    /// Default token expiration time in minutes
    /// </summary>
    public const int DefaultTokenExpirationMinutes = 60;
    
    /// <summary>
    /// Default refresh token expiration time in days
    /// </summary>
    public const int DefaultRefreshTokenExpirationDays = 7;
    
    /// <summary>
    /// Maximum failed login attempts before lockout
    /// </summary>
    public const int MaxFailedLoginAttempts = 3;
    
    /// <summary>
    /// Default account lockout duration in minutes
    /// </summary>
    public const int DefaultLockoutDurationMinutes = 30;
    
    /// <summary>
    /// Default hash iteration count for password hashing
    /// </summary>
    public const int DefaultHashIterationCount = 10000;
    
    /// <summary>
    /// Minimum password history to maintain (prevent password reuse)
    /// </summary>
    public const int PasswordHistoryCount = 5;
    
    /// <summary>
    /// Cookie name for authentication
    /// </summary>
    public const string AuthCookieName = "SmartInsight.Auth";
    
    /// <summary>
    /// System tenant ID (for system-wide resources)
    /// </summary>
    public const string SystemTenantId = "00000000-0000-0000-0000-000000000000";
} 