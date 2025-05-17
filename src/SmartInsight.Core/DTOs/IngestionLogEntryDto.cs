using System;
using System.Text.Json.Serialization;

namespace SmartInsight.Core.DTOs
{
    /// <summary>
    /// Data transfer object for ingestion job log entries
    /// </summary>
    public class IngestionLogEntryDto
    {
        /// <summary>
        /// Gets or sets the log entry ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the ingestion job ID
        /// </summary>
        public Guid IngestionJobId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the log entry
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the log level
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public IngestionLogLevel Level { get; set; }

        /// <summary>
        /// Gets or sets the log message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional details (null if none)
        /// </summary>
        public string? Details { get; set; }
    }

    /// <summary>
    /// Log level for ingestion job logs
    /// </summary>
    public enum IngestionLogLevel
    {
        /// <summary>
        /// Informational message
        /// </summary>
        Info,

        /// <summary>
        /// Warning message
        /// </summary>
        Warning,

        /// <summary>
        /// Error message
        /// </summary>
        Error
    }
} 