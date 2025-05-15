namespace SmartInsight.Core.Security;

/// <summary>
/// Custom claim types for the SmartInsight application
/// </summary>
public static class SmartInsightClaimTypes
{
    private const string ClaimTypeNamespace = "http://smartinsight.com/claims/";

    /// <summary>
    /// Claim type for tenant ID
    /// </summary>
    public const string TenantId = ClaimTypeNamespace + "tenantid";

    /// <summary>
    /// Claim type for user's full name
    /// </summary>
    public const string FullName = ClaimTypeNamespace + "fullname";

    /// <summary>
    /// Claim type for user's first name
    /// </summary>
    public const string FirstName = ClaimTypeNamespace + "firstname";

    /// <summary>
    /// Claim type for user's last name
    /// </summary>
    public const string LastName = ClaimTypeNamespace + "lastname";

    /// <summary>
    /// Claim type for user's active status
    /// </summary>
    public const string IsActive = ClaimTypeNamespace + "isactive";

    /// <summary>
    /// Claim type for permission to access all tenants (admin)
    /// </summary>
    public const string CanAccessAllTenants = ClaimTypeNamespace + "canAccessAllTenants";
} 