using System;

namespace SmartInsight.Telemetry.Options
{
    /// <summary>
    /// Options for configuring the telemetry service
    /// </summary>
    public class TelemetryOptions
    {
        /// <summary>
        /// Whether telemetry is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// Whether to collect detailed metrics
        /// </summary>
        public bool DetailedMetrics { get; set; } = true;
        
        /// <summary>
        /// Whether to anonymize user identifiers
        /// </summary>
        public bool AnonymizeUserData { get; set; } = true;
        
        /// <summary>
        /// Whether to export telemetry to external systems
        /// </summary>
        public bool EnableExport { get; set; } = false;
        
        /// <summary>
        /// The export interval in minutes
        /// </summary>
        public int ExportIntervalMinutes { get; set; } = 60;
        
        /// <summary>
        /// Maximum number of events to retain in memory
        /// </summary>
        public int MaxEventsInMemory { get; set; } = 10000;
        
        /// <summary>
        /// Maximum time to retain events in memory (days)
        /// </summary>
        public int EventRetentionDays { get; set; } = 7;
        
        /// <summary>
        /// Whether to use local storage for telemetry data
        /// </summary>
        public bool UseLocalStorage { get; set; } = true;
        
        /// <summary>
        /// Local storage path for telemetry data
        /// </summary>
        public string LocalStoragePath { get; set; } = "telemetry-data";
        
        /// <summary>
        /// Export endpoints for telemetry data
        /// </summary>
        public ExportEndpointOptions[] ExportEndpoints { get; set; } = Array.Empty<ExportEndpointOptions>();
        
        /// <summary>
        /// The sampling rate for telemetry events (1 = 100%, 0.1 = 10%)
        /// </summary>
        public double SamplingRate { get; set; } = 1.0;
        
        /// <summary>
        /// Whether to include model-specific metrics
        /// </summary>
        public bool IncludeModelMetrics { get; set; } = true;
        
        /// <summary>
        /// Whether to track token usage and costs
        /// </summary>
        public bool TrackTokensAndCosts { get; set; } = true;
        
        /// <summary>
        /// Minimum confidence threshold for logging misclassifications
        /// </summary>
        public double MisclassificationThreshold { get; set; } = 0.7;
        
        /// <summary>
        /// Which event types to include in logging
        /// </summary>
        public EventTypes EventTypesToLog { get; set; } = EventTypes.All;
    }
    
    /// <summary>
    /// Options for configuring an export endpoint
    /// </summary>
    public class ExportEndpointOptions
    {
        /// <summary>
        /// The name of the export endpoint
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// The URL of the export endpoint
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// The export format
        /// </summary>
        public string Format { get; set; } = "json";
        
        /// <summary>
        /// The authentication key or token
        /// </summary>
        public string AuthToken { get; set; } = string.Empty;
        
        /// <summary>
        /// Whether this endpoint is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
    
    /// <summary>
    /// Types of events to include in telemetry logging
    /// </summary>
    [Flags]
    public enum EventTypes
    {
        /// <summary>
        /// No events
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Intent detection events
        /// </summary>
        IntentDetection = 1,
        
        /// <summary>
        /// Fallback events
        /// </summary>
        Fallback = 2,
        
        /// <summary>
        /// User feedback events
        /// </summary>
        UserFeedback = 4,
        
        /// <summary>
        /// Performance metric events
        /// </summary>
        PerformanceMetrics = 8,
        
        /// <summary>
        /// Reasoning events
        /// </summary>
        Reasoning = 16,
        
        /// <summary>
        /// All event types
        /// </summary>
        All = IntentDetection | Fallback | UserFeedback | PerformanceMetrics | Reasoning
    }
} 