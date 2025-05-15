using System;
using System.Collections.Generic;

namespace SmartInsight.Telemetry.Options
{
    /// <summary>
    /// Enum for the rolling interval type
    /// </summary>
    public enum RollingIntervalType
    {
        /// <summary>
        /// Create a new log file on demand, never rolling.
        /// </summary>
        Infinite = 0,
        
        /// <summary>
        /// Roll to a new log file daily.
        /// </summary>
        Day = 1,
        
        /// <summary>
        /// Roll to a new log file hourly.
        /// </summary>
        Hour = 2,
        
        /// <summary>
        /// Roll to a new log file every minute.
        /// </summary>
        Minute = 3
    }
    
    /// <summary>
    /// Options for configuring Serilog
    /// </summary>
    public class SerilogOptions
    {
        /// <summary>
        /// Whether to use Serilog for logging
        /// </summary>
        public bool UseSerilog { get; set; } = true;
        
        /// <summary>
        /// Minimum log level to capture
        /// </summary>
        public string MinimumLevel { get; set; } = "Information";
        
        /// <summary>
        /// Override minimum log levels for specific namespaces
        /// </summary>
        public Dictionary<string, string> OverrideMinimumLevel { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Whether to write logs to the console
        /// </summary>
        public bool WriteToConsole { get; set; } = true;
        
        /// <summary>
        /// Whether to use console logging in development environment
        /// </summary>
        public bool UseConsoleInDevelopment { get; set; } = true;
        
        /// <summary>
        /// Whether to write logs to files
        /// </summary>
        public bool WriteToFile { get; set; } = true;
        
        /// <summary>
        /// File log path
        /// </summary>
        public string FilePath { get; set; } = "logs/smartinsight-.log";
        
        /// <summary>
        /// The interval at which to roll the log file (0=Infinite, 1=Day, 2=Hour, 3=Minute)
        /// </summary>
        public RollingIntervalType RollingInterval { get; set; } = RollingIntervalType.Day;
        
        /// <summary>
        /// Maximum file size in megabytes
        /// </summary>
        public int? FileSizeLimitMB { get; set; } = 10;
        
        /// <summary>
        /// Maximum number of log files to retain
        /// </summary>
        public int? RetainedFileCount { get; set; } = 31;
        
        /// <summary>
        /// Whether to write to Seq server
        /// </summary>
        public bool WriteToSeq { get; set; } = false;
        
        /// <summary>
        /// Seq server URL
        /// </summary>
        public string SeqServerUrl { get; set; } = "http://localhost:5341";
        
        /// <summary>
        /// Seq API key
        /// </summary>
        public string SeqApiKey { get; set; } = "";
        
        /// <summary>
        /// Whether to enrich logs with machine name
        /// </summary>
        public bool EnrichWithMachineName { get; set; } = true;
        
        /// <summary>
        /// Whether to enrich logs with environment information
        /// </summary>
        public bool EnrichWithEnvironment { get; set; } = true;
        
        /// <summary>
        /// Whether to enrich logs with thread information
        /// </summary>
        public bool EnrichWithThreadId { get; set; } = true;
        
        /// <summary>
        /// Whether to enrich logs with context data
        /// </summary>
        public bool EnrichWithContext { get; set; } = true;
        
        /// <summary>
        /// Flag to preserve static logger configuration after application exit
        /// </summary>
        public bool PreserveStaticLogger { get; set; } = false;
    }
} 