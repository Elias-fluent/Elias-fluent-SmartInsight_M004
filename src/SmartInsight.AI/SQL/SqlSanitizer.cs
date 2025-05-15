using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Provides SQL sanitization and injection prevention
    /// </summary>
    public class SqlSanitizer : ISqlSanitizer
    {
        private readonly ILogger<SqlSanitizer> _logger;
        
        // SQL injection patterns to detect
        private static readonly string[] _sqlInjectionPatterns = new[]
        {
            @";\s*--",                          // Inline comment
            @";\s*\/\*.*?\*\/",                 // Block comment
            @"UNION\s+ALL\s+SELECT",            // UNION injection
            @"OR\s+['""]?\d+['""]?\s*=\s*['""]?\d+['""]?", // OR 1=1
            @"DROP\s+TABLE",                    // DROP TABLE
            @"DELETE\s+FROM",                   // DELETE FROM
            @"INSERT\s+INTO",                   // INSERT INTO
            @"EXEC\s*\(",                       // EXEC(
            @"EXECUTE\s*\(",                    // EXECUTE(
            @"xp_cmdshell",                     // xp_cmdshell
            @"sp_execute",                      // sp_execute
            @"--",                              // SQL comment
            @"\/\*|\*\/",                       // Block comment markers
            @";\s*SELECT",                      // Statement chaining
            @";\s*UPDATE",                      // Statement chaining
            @";\s*INSERT",                      // Statement chaining
            @";\s*DELETE"                       // Statement chaining
        };
        
        // Allowed SQL operations for templates
        private static readonly string[] _allowedSqlOperations = new[]
        {
            "SELECT", "INSERT", "UPDATE", "DELETE", "JOIN", "WHERE", "GROUP BY", "ORDER BY", 
            "HAVING", "LIMIT", "OFFSET", "DISTINCT", "FROM", "TOP", "SET", "VALUES", "AS",
            "AND", "OR", "NOT", "IN", "BETWEEN", "LIKE", "IS NULL", "IS NOT NULL", "COUNT",
            "AVG", "SUM", "MIN", "MAX", "LEFT JOIN", "RIGHT JOIN", "INNER JOIN", "OUTER JOIN"
        };
        
        // Disallowed SQL keywords that shouldn't be in user templates
        private static readonly string[] _disallowedKeywords = new[]
        {
            "DROP", "TRUNCATE", "ALTER", "CREATE", "MODIFY", "RENAME", "EXEC", "EXECUTE",
            "xp_", "sp_", "OPENQUERY", "OPENROWSET", "BULK INSERT", "RECONFIGURE", "SHUTDOWN"
        };
        
        /// <summary>
        /// Creates a new SqlSanitizer
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public SqlSanitizer(ILogger<SqlSanitizer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public Task<ParameterizedSqlResult> ParameterizeSqlAsync(
            string sql,
            Dictionary<string, object>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return Task.FromResult(new ParameterizedSqlResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "SQL query cannot be empty"
                });
            }
            
            try
            {
                // Check if SQL contains any dangerous patterns before parameterization
                if (ContainsSqlInjectionPatterns(sql))
                {
                    _logger.LogWarning("SQL query contains potential injection patterns: {SqlQuery}", sql);
                    return Task.FromResult(new ParameterizedSqlResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = "SQL query contains potential SQL injection patterns"
                    });
                }

                // Check for disallowed keywords
                foreach (var keyword in _disallowedKeywords)
                {
                    if (Regex.IsMatch(sql, $@"\b{keyword}\b", RegexOptions.IgnoreCase))
                    {
                        _logger.LogWarning("SQL query contains disallowed keyword '{Keyword}': {SqlQuery}", keyword, sql);
                        return Task.FromResult(new ParameterizedSqlResult
                        {
                            IsSuccessful = false,
                            ErrorMessage = $"SQL query contains disallowed keyword: {keyword}"
                        });
                    }
                }
                
                // Create a copy of the parameters or initialize if null
                var parametersCopy = parameters != null 
                    ? new Dictionary<string, object>(parameters) 
                    : new Dictionary<string, object>();
                
                // Initial sanitized SQL
                var sanitizedSql = sql;
                
                // Replace any literal values with parameters
                sanitizedSql = ParameterizeLiteralValues(sanitizedSql, parametersCopy);
                
                return Task.FromResult(new ParameterizedSqlResult
                {
                    IsSuccessful = true,
                    Sql = sanitizedSql,
                    Parameters = parametersCopy
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parameterizing SQL query: {SqlQuery}", sql);
                return Task.FromResult(new ParameterizedSqlResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Error parameterizing SQL query: {ex.Message}"
                });
            }
        }
        
        /// <inheritdoc />
        public string EscapeSqlValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            
            // Replace single quotes with double quotes (standard SQL escaping)
            return value.Replace("'", "''");
        }
        
        /// <inheritdoc />
        public string SanitizeSqlIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return string.Empty;
            }
            
            // Remove any characters that aren't allowed in SQL identifiers
            // Only allow letters, numbers, and underscores
            var sanitized = Regex.Replace(identifier, @"[^\w]", "");
            
            // Ensure the identifier starts with a letter (required for SQL identifiers)
            if (sanitized.Length > 0 && !char.IsLetter(sanitized[0]))
            {
                sanitized = "i" + sanitized;
            }
            
            // If the identifier got completely removed, return a safe default
            if (string.IsNullOrEmpty(sanitized))
            {
                return "identifier";
            }
            
            return sanitized;
        }
        
        /// <inheritdoc />
        public string SanitizeSqlQuery(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return string.Empty;
            }
            
            // Remove comments
            var noComments = Regex.Replace(sql, @"--.*?$", "", RegexOptions.Multiline);
            noComments = Regex.Replace(noComments, @"/\*.*?\*/", "", RegexOptions.Singleline);
            
            // Remove multiple semicolons (prevent multiple statement execution)
            var noMultiStatements = Regex.Replace(noComments, @";\s*;", ";");
            
            // Check for and remove disallowed keywords
            var sanitized = noMultiStatements;
            foreach (var keyword in _disallowedKeywords)
            {
                sanitized = Regex.Replace(
                    sanitized, 
                    $@"\b{keyword}\b", 
                    "[REMOVED]", 
                    RegexOptions.IgnoreCase);
            }
            
            return sanitized.Trim();
        }
        
        /// <inheritdoc />
        public bool ContainsSqlInjectionPatterns(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            
            return _sqlInjectionPatterns.Any(pattern => 
                Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase));
        }
        
        /// <inheritdoc />
        public IReadOnlyList<string> GetAllowedSqlOperations()
        {
            return _allowedSqlOperations;
        }
        
        /// <summary>
        /// Replaces literal values in SQL with parameterized values
        /// </summary>
        /// <param name="sql">The SQL query</param>
        /// <param name="parameters">Dictionary to add parameters to</param>
        /// <returns>SQL with literals converted to parameters</returns>
        private string ParameterizeLiteralValues(string sql, Dictionary<string, object> parameters)
        {
            // Replace string literals (e.g., 'value')
            var stringPattern = @"'((?:[^']|'')*)'";
            var stringMatches = Regex.Matches(sql, stringPattern);
            
            var resultSql = sql;
            
            foreach (Match match in stringMatches)
            {
                var value = match.Groups[1].Value.Replace("''", "'");
                var paramName = $"@p{parameters.Count}";
                
                parameters[paramName] = value;
                resultSql = resultSql.Replace(match.Value, paramName);
            }
            
            // Replace numeric literals not part of identifiers or existing parameters
            // This regex looks for numbers not preceded by @ (parameter) or followed/preceded by letters/underscore (identifier)
            var numericPattern = @"(?<!\w|@)(\d+(?:\.\d+)?)(?!\w)";
            var numericMatches = Regex.Matches(resultSql, numericPattern);
            
            foreach (Match match in numericMatches)
            {
                var value = match.Groups[1].Value;
                // Try to preserve numeric type (int vs decimal)
                object paramValue;
                if (value.Contains("."))
                {
                    if (decimal.TryParse(value, out var decimalValue))
                    {
                        paramValue = decimalValue;
                    }
                    else
                    {
                        continue; // Skip if not a valid decimal
                    }
                }
                else
                {
                    if (int.TryParse(value, out var intValue))
                    {
                        paramValue = intValue;
                    }
                    else
                    {
                        continue; // Skip if not a valid int
                    }
                }
                
                var paramName = $"@p{parameters.Count}";
                parameters[paramName] = paramValue;
                
                // Replace just this occurrence
                resultSql = resultSql.Substring(0, match.Index) + paramName + resultSql.Substring(match.Index + match.Length);
            }
            
            return resultSql;
        }
    }
} 