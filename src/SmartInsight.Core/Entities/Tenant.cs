using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents an organization unit with isolation boundaries and security settings
/// </summary>
public class Tenant : BaseEntity
{
    /// <summary>
    /// Unique name of the tenant
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the tenant
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact email for the tenant
    /// </summary>
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Description of the tenant organization
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the tenant is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Unique identifier used for database Row-Level Security policies
    /// </summary>
    [Required]
    public string RlsIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Optional parent tenant for hierarchical tenant structures
    /// </summary>
    public Guid? ParentTenantId { get; set; }

    /// <summary>
    /// Navigation property to parent tenant
    /// </summary>
    [JsonIgnore]
    public Tenant? ParentTenant { get; set; }

    /// <summary>
    /// Tenant-specific configuration settings stored as JSON
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Navigation property to users associated with this tenant
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<User>? Users { get; set; }

    /// <summary>
    /// Navigation property to data sources owned by this tenant
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<DataSource>? DataSources { get; set; }
} 