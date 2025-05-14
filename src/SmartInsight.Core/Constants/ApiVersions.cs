namespace SmartInsight.Core.Constants;

/// <summary>
/// Constants for API versioning
/// </summary>
public static class ApiVersions
{
    /// <summary>
    /// Version 1.0 - Initial API
    /// </summary>
    public const string V1 = "1.0";
    
    /// <summary>
    /// Version 2.0 - Enhanced API with additional endpoints
    /// </summary>
    public const string V2 = "2.0";
    
    /// <summary>
    /// Default API version to use when not specified
    /// </summary>
    public const string Default = V1;
    
    /// <summary>
    /// API route prefix format
    /// </summary>
    public const string RoutePrefix = "api/v{version:apiVersion}";
} 