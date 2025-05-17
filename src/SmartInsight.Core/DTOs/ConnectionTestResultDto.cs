using System;
using System.Collections.Generic;

namespace SmartInsight.Core.DTOs
{
    /// <summary>
    /// Data transfer object for connection test results
    /// </summary>
    public class ConnectionTestResultDto
    {
        /// <summary>
        /// Gets or sets whether the connection was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Gets or sets the error message (null if successful)
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets detailed connection information
        /// </summary>
        public IDictionary<string, string>? ConnectionDetails { get; set; }
        
        /// <summary>
        /// Gets or sets the response time in milliseconds
        /// </summary>
        public long ResponseTimeMs { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp of the test
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