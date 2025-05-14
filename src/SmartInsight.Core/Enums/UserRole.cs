namespace SmartInsight.Core.Enums;

/// <summary>
/// User roles within the system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// System administrator with full access
    /// </summary>
    SystemAdmin,
    
    /// <summary>
    /// Tenant administrator for a specific organization
    /// </summary>
    TenantAdmin,
    
    /// <summary>
    /// Data administrator who can configure data sources
    /// </summary>
    DataAdmin,
    
    /// <summary>
    /// Standard user with basic permissions
    /// </summary>
    User,
    
    /// <summary>
    /// Read-only user with limited access
    /// </summary>
    ReadOnly,
    
    /// <summary>
    /// Guest user with minimal permissions
    /// </summary>
    Guest,
    
    /// <summary>
    /// API access only, for service accounts
    /// </summary>
    ApiUser
} 