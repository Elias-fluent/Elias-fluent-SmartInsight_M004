using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartInsight.Core.DTOs
{
    /// <summary>
    /// Data transfer object for ingestion jobs
    /// </summary>
    public class IngestionJobDto
    {
        /// <summary>
        /// Gets or sets the job ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the data source ID
        /// </summary>
        public Guid DataSourceId { get; set; }

        /// <summary>
        /// Gets or sets the data source name
        /// </summary>
        public string? DataSourceName { get; set; }

        /// <summary>
        /// Gets or sets the job start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the job end time (null if not completed)
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Gets or sets the job status
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public IngestionJobStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the job progress (0-100)
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// Gets or sets the number of records processed
        /// </summary>
        public int RecordsProcessed { get; set; }

        /// <summary>
        /// Gets or sets the total number of records to process (null if unknown)
        /// </summary>
        public int? TotalRecords { get; set; }

        /// <summary>
        /// Gets or sets the error message (null if no error)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the log entries for the job
        /// </summary>
        public List<IngestionLogEntryDto>? LogEntries { get; set; }

        /// <summary>
        /// Gets the duration of the job
        /// </summary>
        public TimeSpan Duration => EndTime.HasValue 
            ? EndTime.Value - StartTime 
            : DateTime.UtcNow - StartTime;
    }

    /// <summary>
    /// Status of an ingestion job
    /// </summary>
    public enum IngestionJobStatus
    {
        /// <summary>
        /// Job is queued for processing
        /// </summary>
        Queued,

        /// <summary>
        /// Job is running
        /// </summary>
        Running,

        /// <summary>
        /// Job completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Job failed
        /// </summary>
        Failed,

        /// <summary>
        /// Job was cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Job is paused
        /// </summary>
        Paused
    }
} 