using SmartInsight.Core.Enums;
using System.Text.Json.Serialization;

namespace SmartInsight.Core.DTOs;

/// <summary>
/// DTO for data source information
/// </summary>
public record DataSourceDto
{
    /// <summary>
    /// Unique identifier for the data source
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Name of the data source
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of the data source
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of data source
    /// </summary>
    public DataSourceType SourceType { get; init; }
    
    /// <summary>
    /// Tenant ID that owns this data source
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the data source is currently active
    /// </summary>
    public bool IsActive { get; init; } = true;
    
    /// <summary>
    /// Connection parameters for the data source (encrypted)
    /// </summary>
    [JsonIgnore]
    public IDictionary<string, string> ConnectionParameters { get; init; } = new Dictionary<string, string>();
    
    /// <summary>
    /// Last successful refresh timestamp
    /// </summary>
    public DateTimeOffset? LastRefreshed { get; init; }
    
    /// <summary>
    /// Refresh schedule in cron format
    /// </summary>
    public string RefreshSchedule { get; init; } = string.Empty;
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// DTO for creating a new data source
/// </summary>
public record CreateDataSourceDto
{
    /// <summary>
    /// Name of the data source
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Description of the data source
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Type of data source
    /// </summary>
    public required DataSourceType SourceType { get; init; }
    
    /// <summary>
    /// Tenant ID that owns this data source
    /// </summary>
    public required string TenantId { get; init; }
    
    /// <summary>
    /// Connection parameters for the data source
    /// </summary>
    public required IDictionary<string, string> ConnectionParameters { get; init; }
    
    /// <summary>
    /// Refresh schedule in cron format
    /// </summary>
    public string? RefreshSchedule { get; init; }
}

/// <summary>
/// DTO for updating an existing data source
/// </summary>
public record UpdateDataSourceDto
{
    /// <summary>
    /// Name of the data source
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// Description of the data source
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Whether the data source is currently active
    /// </summary>
    public bool? IsActive { get; init; }
    
    /// <summary>
    /// Connection parameters for the data source
    /// </summary>
    public IDictionary<string, string>? ConnectionParameters { get; init; }
    
    /// <summary>
    /// Refresh schedule in cron format
    /// </summary>
    public string? RefreshSchedule { get; init; }
} 