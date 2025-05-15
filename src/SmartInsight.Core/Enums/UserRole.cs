namespace SmartInsight.Core.Enums;

/// <summary>
/// Represents the roles a user can have within the system
/// </summary>
public enum UserRole
{
    /// <summary>Regular user who can query the system</summary>
    User = 1,
    
    /// <summary>Power user with additional permissions</summary>
    PowerUser = 2,
    
    /// <summary>Tenant administrator with tenant management permissions</summary>
    TenantAdmin = 3,
    
    /// <summary>System administrator with full system access</summary>
    SystemAdmin = 4,
    
    /// <summary>
    /// Data administrator who can configure data sources
    /// </summary>
    DataAdmin,
    
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