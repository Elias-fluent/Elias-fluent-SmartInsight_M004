using System.ComponentModel.DataAnnotations;

namespace SmartInsight.Core.DTOs;

/// <summary>
/// Data transfer object for token refresh requests
/// </summary>
public class RefreshTokenDto
{
    /// <summary>
    /// The current access token
    /// </summary>
    [Required]
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// The refresh token to use for getting a new access token
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
} 