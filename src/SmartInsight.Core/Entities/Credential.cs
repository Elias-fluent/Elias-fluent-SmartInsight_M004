using System.ComponentModel.DataAnnotations;using System.ComponentModel.DataAnnotations.Schema;namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a securely stored credential in the system
/// </summary>
public class Credential : BaseMultiTenantEntity
{
    /// <summary>
    /// Unique key for the credential within a tenant
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Encrypted value of the credential
    /// </summary>
    [Required]
    public string EncryptedValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Initialization vector used for encryption
    /// </summary>
    [Required]
    public string IV { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional metadata about this credential (JSON string)
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Optional source of this credential (e.g., connector ID)
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }
    
    /// <summary>
    /// Optional group this credential belongs to for organization
    /// </summary>
    [MaxLength(100)]
    public string? Group { get; set; }
    
    /// <summary>
    /// Expiration date of this credential
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>
    /// Number of times this credential has been accessed
    /// </summary>
    public int AccessCount { get; set; } = 0;
    
    /// <summary>
    /// Last time this credential was accessed
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }
    
    /// <summary>
    /// Rotation history for audit purposes (JSON array)
    /// </summary>
    public string? RotationHistory { get; set; }
    
    /// <summary>
    /// Last rotation date
    /// </summary>
    public DateTime? LastRotatedAt { get; set; }
    
    /// <summary>
    /// Whether this credential is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// When the credential was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
} 