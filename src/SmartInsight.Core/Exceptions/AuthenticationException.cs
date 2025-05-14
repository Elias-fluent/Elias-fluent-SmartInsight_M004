namespace SmartInsight.Core.Exceptions;

/// <summary>
/// Exception for authentication and authorization errors
/// </summary>
public class AuthenticationException : SmartInsightException
{
    /// <summary>
    /// Username that failed authentication (if available)
    /// </summary>
    public string? Username { get; }
    
    /// <summary>
    /// Tenant ID related to the authentication error (if available)
    /// </summary>
    public string? TenantId { get; }
    
    /// <summary>
    /// Creates a new authentication exception
    /// </summary>
    public AuthenticationException() 
        : base("Authentication failed.", "AUTH_ERROR")
    {
    }
    
    /// <summary>
    /// Creates a new authentication exception with a message
    /// </summary>
    /// <param name="message">Exception message</param>
    public AuthenticationException(string message) 
        : base(message, "AUTH_ERROR")
    {
    }
    
    /// <summary>
    /// Creates a new authentication exception with a message and username
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="username">Username that failed authentication</param>
    public AuthenticationException(string message, string username) 
        : base(message, "AUTH_ERROR")
    {
        Username = username;
    }
    
    /// <summary>
    /// Creates a new authentication exception with a message, username, and tenant ID
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="username">Username that failed authentication</param>
    /// <param name="tenantId">Tenant ID related to the authentication error</param>
    public AuthenticationException(string message, string username, string tenantId) 
        : base(message, "AUTH_ERROR")
    {
        Username = username;
        TenantId = tenantId;
    }
    
    /// <summary>
    /// Creates a new authentication exception with a message and inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception</param>
    public AuthenticationException(string message, Exception innerException) 
        : base(message, "AUTH_ERROR", innerException)
    {
    }
} 