namespace SmartInsight.Core.DTOs;

/// <summary>
/// Data transfer object for JWT tokens
/// </summary>
public class TokenDto
{
    /// <summary>
    /// The JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// The token type (usually "Bearer")
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// Expiration time in seconds
    /// </summary>
    public long ExpiresIn { get; set; }
    
    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Refresh token expiration time in seconds
    /// </summary>
    public long RefreshTokenExpiresIn { get; set; }
} 