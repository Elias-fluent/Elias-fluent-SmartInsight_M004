using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SmartInsight.Core.Enums;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a user identity with role and permission claims
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Username for the user (unique within the system)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the user
    /// </summary>
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

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
    /// Hash of the user's password (when using local authentication)
    /// </summary>
    [JsonIgnore]
    public string? PasswordHash { get; set; }

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
    /// Whether the user is active and can log in
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the user's email has been verified
    /// </summary>
    public bool EmailVerified { get; set; } = false;

    /// <summary>
    /// When the user last logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Role of the user in the system
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// Primary tenant for this user
    /// </summary>
    public Guid PrimaryTenantId { get; set; }

    /// <summary>
    /// Navigation property to the primary tenant
    /// </summary>
    [ForeignKey("PrimaryTenantId")]
    public virtual Tenant? PrimaryTenant { get; set; }

    /// <summary>
    /// User-specific settings stored as JSON
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Navigation property to conversation logs initiated by this user
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<ConversationLog>? ConversationLogs { get; set; }

    /// <summary>
    /// Full name of the user (computed property)
    /// </summary>
    [NotMapped]
    public string FullName => string.IsNullOrEmpty(DisplayName) 
        ? $"{FirstName} {LastName}".Trim() 
        : DisplayName;
} 