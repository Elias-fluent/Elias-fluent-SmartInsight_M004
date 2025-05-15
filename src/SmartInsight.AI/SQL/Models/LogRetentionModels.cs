using System;
using System.Collections.Generic;

namespace SmartInsight.AI.SQL.Models
{
    /// <summary>
    /// Types of SQL logs
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Query generation logs
        /// </summary>
        QueryGeneration,
        
        /// <summary>
        /// Query execution logs
        /// </summary>
        QueryExecution,
        
        /// <summary>
        /// Template selection logs
        /// </summary>
        TemplateSelection,
        
        /// <summary>
        /// Parameter validation logs
        /// </summary>
        ParameterValidation,
        
        /// <summary>
        /// Error logs
        /// </summary>
        Error,
        
        /// <summary>
        /// Security logs
        /// </summary>
        Security,
        
        /// <summary>
        /// Performance logs
        /// </summary>
        Performance,
        
        /// <summary>
        /// Audit logs
        /// </summary>
        Audit
    }
    
    /// <summary>
    /// Result of a log cleanup operation
    /// </summary>
    public class LogCleanupResult
    {
        /// <summary>
        /// Number of logs deleted
        /// </summary>
        public long LogsDeleted { get; set; }
        
        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Whether the cleanup was successful
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// Error message if cleanup failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Number of logs deleted by type
        /// </summary>
        public Dictionary<LogType, long> LogsDeletedByType { get; set; } = new Dictionary<LogType, long>();
        
        /// <summary>
        /// Timestamp of the oldest log after cleanup
        /// </summary>
        public DateTime? OldestLogAfterCleanup { get; set; }
    }
    
    /// <summary>
    /// SQL log retention options
    /// </summary>
    public class SqlLogRetentionOptions
    {
        /// <summary>
        /// Default retention days for all log types
        /// </summary>
        public int DefaultRetentionDays { get; set; } = 30;
        
        /// <summary>
        /// Retention days for error logs
        /// </summary>
        public int ErrorLogRetentionDays { get; set; } = 90;
        
        /// <summary>
        /// Retention days for performance logs
        /// </summary>
        public int PerformanceLogRetentionDays { get; set; } = 60;
        
        /// <summary>
        /// Retention days for security logs
        /// </summary>
        public int SecurityLogRetentionDays { get; set; } = 90;
        
        /// <summary>
        /// Retention days for audit logs
        /// </summary>
        public int AuditLogRetentionDays { get; set; } = 365;
        
        /// <summary>
        /// Batch size for query operations
        /// </summary>
        public int QueryBatchSize { get; set; } = 100;
        
        /// <summary>
        /// Maximum number of logs to delete in a single run
        /// </summary>
        public int MaxLogsToDeletePerRun { get; set; } = 1000;
        
        /// <summary>
        /// Whether to compress logs instead of deleting them
        /// </summary>
        public bool CompressInsteadOfDelete { get; set; } = false;
    }
} 