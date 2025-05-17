using System;
using System.Collections.Generic;

namespace SmartInsight.Core.DTOs
{
    /// <summary>
    /// DTO for connection test results
    /// </summary>
    public class ConnectionTestResultDto
    {
        /// <summary>
        /// Whether the connection test was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Error message if the test failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Details about the connection
        /// </summary>
        public IDictionary<string, string>? ConnectionDetails { get; set; }
        
        /// <summary>
        /// Time it took to test the connection in milliseconds
        /// </summary>
        public long ResponseTimeMs { get; set; }
        
        /// <summary>
        /// Timestamp of the test
        /// </summary>
        public DateTimeOffset TestTimestamp { get; set; } = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// DTO for testing a connection
    /// </summary>
    public class TestConnectionDto
    {
        /// <summary>
        /// Type of data source to test
        /// </summary>
        public required Core.Enums.DataSourceType SourceType { get; set; }
        
        /// <summary>
        /// Connection parameters for the test
        /// </summary>
        public required IDictionary<string, string> ConnectionParameters { get; set; }
    }
} 