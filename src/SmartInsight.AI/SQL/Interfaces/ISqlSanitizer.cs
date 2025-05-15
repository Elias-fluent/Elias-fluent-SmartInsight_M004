using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for SQL sanitization and injection prevention
    /// </summary>
    public interface ISqlSanitizer
    {
        /// <summary>
        /// Parameterizes a SQL query to prevent SQL injection
        /// </summary>
        /// <param name="sql">The SQL query to parameterize</param>
        /// <param name="parameters">Parameters to use in the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Parameterized SQL query and prepared parameters</returns>
        Task<ParameterizedSqlResult> ParameterizeSqlAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Escapes a string value for safe inclusion in SQL
        /// </summary>
        /// <param name="value">The value to escape</param>
        /// <returns>The escaped value</returns>
        string EscapeSqlValue(string value);
        
        /// <summary>
        /// Sanitizes a SQL identifier (table name, column name, etc.)
        /// </summary>
        /// <param name="identifier">The identifier to sanitize</param>
        /// <returns>The sanitized identifier</returns>
        string SanitizeSqlIdentifier(string identifier);
        
        /// <summary>
        /// Sanitizes an entire SQL query by removing dangerous elements
        /// </summary>
        /// <param name="sql">The SQL query to sanitize</param>
        /// <returns>The sanitized SQL query</returns>
        string SanitizeSqlQuery(string sql);
        
        /// <summary>
        /// Checks if a string contains SQL injection patterns
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <returns>True if the value contains SQL injection patterns</returns>
        bool ContainsSqlInjectionPatterns(string value);
        
        /// <summary>
        /// Gets a whitelist of allowed SQL operations for use in templates
        /// </summary>
        /// <returns>A list of allowed SQL operations</returns>
        IReadOnlyList<string> GetAllowedSqlOperations();
    }
    
    /// <summary>
    /// Result of parameterizing a SQL query
    /// </summary>
    public class ParameterizedSqlResult
    {
        /// <summary>
        /// The parameterized SQL
        /// </summary>
        public string Sql { get; set; } = null!;
        
        /// <summary>
        /// The parameterized SQL (alias for Sql for backward compatibility)
        /// </summary>
        public string ParameterizedSql => Sql;
        
        /// <summary>
        /// The parameters to use with the query
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Whether the parameterization was successful
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// Error message if parameterization failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
} 