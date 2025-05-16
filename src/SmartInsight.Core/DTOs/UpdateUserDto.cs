using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartInsight.Core.DTOs;

/// <summary>
/// Data transfer object for updating existing users
/// </summary>
public class UpdateUserDto
{
    /// <summary>
    /// Username
    /// </summary>
    [StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Password (optional - only updated if provided)
    /// </summary>
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name or full name
    /// </summary>
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the account is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// User roles (replaces existing roles if provided)
    /// </summary>
    public List<string> Roles { get; set; } = new List<string>();
} 