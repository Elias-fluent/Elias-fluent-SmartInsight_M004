using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Application user entity that extends ASP.NET Identity's IdentityUser
/// with tenant-specific properties and overrides 
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// First name of the user
    /// </summary>
    [MaxLength(100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name of the user
    /// </summary>
    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Display name shown in the UI
    /// </summary>
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Whether the user is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created the user
    /// </summary>
    [MaxLength(256)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the user was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who last updated the user
    /// </summary>
    [MaxLength(256)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When the user last logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// External identity provider ID (for OAuth/SSO logins)
    /// </summary>
    [MaxLength(256)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Name of the external identity provider (e.g., "Google", "Microsoft")
    /// </summary>
    [MaxLength(50)]
    public string? ExternalProvider { get; set; }

    /// <summary>
    /// Primary tenant for this user
    /// </summary>
    public Guid PrimaryTenantId { get; set; }

    /// <summary>
    /// User-specific settings stored as JSON
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Full name of the user (computed property)
    /// </summary>
    public string FullName => string.IsNullOrEmpty(DisplayName) 
        ? $"{FirstName} {LastName}".Trim() 
        : DisplayName;
} 