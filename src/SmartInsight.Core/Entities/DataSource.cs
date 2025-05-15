using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SmartInsight.Core.Enums;

namespace SmartInsight.Core.Entities;

/// <summary>
/// Represents a connection to an external data source from which information is extracted
/// </summary>
public class DataSource : BaseMultiTenantEntity
{
    /// <summary>
    /// Name of the data source
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the data source
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of the data source
    /// </summary>
    [Required]
    public DataSourceType Type { get; set; }

    /// <summary>
    /// Connection string or URL for the data source (encrypted at rest)
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Username for authentication with the data source
    /// </summary>
    [MaxLength(100)]
    public string? Username { get; set; }

    /// <summary>
    /// Password for authentication with the data source (encrypted at rest)
    /// </summary>
    [JsonIgnore]
    public string? Password { get; set; }

    /// <summary>
    /// API key or token for authentication with the data source (encrypted at rest)
    /// </summary>
    [JsonIgnore]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Additional connection parameters stored as JSON
    /// </summary>
    public string? ConnectionParameters { get; set; }

    /// <summary>
    /// How often to refresh data from this source (in minutes)
    /// </summary>
    public int RefreshInterval { get; set; } = 1440; // Default to daily

    /// <summary>
    /// When the data source was last successfully accessed
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// When the next scheduled refresh should occur
    /// </summary>
    public DateTime? NextRefreshAt { get; set; }

    /// <summary>
    /// Whether the data source is enabled for ingestion
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Status message from the last ingestion attempt
    /// </summary>
    [MaxLength(500)]
    public string? LastStatusMessage { get; set; }

    /// <summary>
    /// Navigation property to documents extracted from this data source
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<Document>? Documents { get; set; }

    /// <summary>
    /// Custom connector type name (if Type is Custom)
    /// </summary>
    [MaxLength(100)]
    public string? CustomConnectorType { get; set; }

    /// <summary>
    /// Version of the connector used for this data source
    /// </summary>
    [MaxLength(20)]
    public string? ConnectorVersion { get; set; }

    /// <summary>
    /// Maximum amount of data to ingest per refresh (in MB, 0 for unlimited)
    /// </summary>
    public int MaxIngestionSize { get; set; } = 0;
} 