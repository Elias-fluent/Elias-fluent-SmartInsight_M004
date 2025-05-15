using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Validators
{
    /// <summary>
    /// Specialized validator for database object parameters (tables, columns, schemas)
    /// </summary>
    public class DatabaseObjectValidator
    {
        private readonly IParameterValidator _baseValidator;
        private readonly ILogger<DatabaseObjectValidator> _logger;
        
        // Common database object naming regex pattern
        private const string ValidDatabaseIdentifierPattern = @"^[a-zA-Z][a-zA-Z0-9_]*$";
        
        // List of SQL reserved words that should be avoided for database objects
        private static readonly HashSet<string> _sqlReservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ADD", "ALTER", "ALL", "AND", "ANY", "AS", "ASC", "BETWEEN", "BY", "CASE", 
            "CHECK", "COLUMN", "CONSTRAINT", "CREATE", "DATABASE", "DEFAULT", "DELETE", 
            "DESC", "DISTINCT", "DROP", "EXEC", "EXISTS", "FOREIGN", "FROM", "FULL", 
            "GROUP", "HAVING", "IN", "INDEX", "INNER", "INSERT", "INTO", "IS", "JOIN", 
            "KEY", "LEFT", "LIKE", "LIMIT", "NOT", "NULL", "OR", "ORDER", "OUTER", 
            "PRIMARY", "PROCEDURE", "RIGHT", "ROWNUM", "SELECT", "SET", "TABLE", 
            "TOP", "TRUNCATE", "UNION", "UNIQUE", "UPDATE", "VALUES", "VIEW", "WHERE"
        };
        
        /// <summary>
        /// Creates a new instance of DatabaseObjectValidator
        /// </summary>
        /// <param name="baseValidator">The base parameter validator</param>
        /// <param name="logger">Logger instance</param>
        public DatabaseObjectValidator(
            IParameterValidator baseValidator,
            ILogger<DatabaseObjectValidator> logger)
        {
            _baseValidator = baseValidator ?? throw new ArgumentNullException(nameof(baseValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Validates database object parameters
        /// </summary>
        /// <param name="parameters">The parameters to validate</param>
        /// <param name="template">The SQL template</param>
        /// <returns>The validation result</returns>
        public async Task<Models.ParameterValidationResult> ValidateDatabaseObjectParametersAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template)
        {
            // Start with base validation
            var result = await _baseValidator.ValidateParametersAsync(parameters, template);
            
            // If already invalid, no need for additional validation
            if (!result.IsValid)
            {
                return result;
            }
            
            // Check database object naming conventions for all parameters that might be database identifiers
            foreach (var param in parameters)
            {
                if (IsLikelyDatabaseObject(param.Key) && param.Value.Value is string objectName)
                {
                    // Validate against naming pattern
                    if (!Regex.IsMatch(objectName, ValidDatabaseIdentifierPattern))
                    {
                        result.AddIssue(new ParameterValidationIssue
                        {
                            ParameterName = param.Key,
                            RuleName = "Database.InvalidIdentifier",
                            Description = $"Database identifier '{objectName}' does not follow naming conventions",
                            Severity = ValidationSeverity.Warning,
                            OriginalValue = objectName,
                            Recommendation = "Database object names should start with a letter and contain only letters, numbers, and underscores"
                        });
                    }
                    
                    // Check for SQL reserved words
                    if (_sqlReservedWords.Contains(objectName))
                    {
                        result.AddIssue(new ParameterValidationIssue
                        {
                            ParameterName = param.Key,
                            RuleName = "Database.ReservedWord",
                            Description = $"'{objectName}' is a SQL reserved word and should be avoided for database objects",
                            Severity = ValidationSeverity.Warning,
                            OriginalValue = objectName,
                            Recommendation = "Choose a different name that is not a SQL reserved word"
                        });
                    }
                    
                    // Check for injection attempts in object names
                    if (ContainsDatabaseInjectionPatterns(objectName))
                    {
                        result.AddIssue(new ParameterValidationIssue
                        {
                            ParameterName = param.Key,
                            RuleName = "Security.ObjectNameInjection",
                            Description = $"Database object name '{objectName}' contains potential SQL injection patterns",
                            Severity = ValidationSeverity.Critical,
                            OriginalValue = objectName,
                            Recommendation = "Provide a valid database object name without special characters or SQL syntax"
                        });
                    }
                }
            }
            
            // Special validation for schema names
            if (parameters.TryGetValue("schema", out var schemaParam) && schemaParam.Value is string schemaName)
            {
                // Allowed schemas list (example)
                var allowedSchemas = new[] { "dbo", "public", "app", "data", "users" };
                if (!allowedSchemas.Contains(schemaName, StringComparer.OrdinalIgnoreCase))
                {
                    result.AddIssue(new ParameterValidationIssue
                    {
                        ParameterName = "schema",
                        RuleName = "Database.UnknownSchema",
                        Description = $"Schema '{schemaName}' is not in the list of allowed schemas",
                        Severity = ValidationSeverity.Warning,
                        OriginalValue = schemaName,
                        Recommendation = $"Use one of the allowed schemas: {string.Join(", ", allowedSchemas)}"
                    });
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Determines if a parameter name likely refers to a database object
        /// </summary>
        private bool IsLikelyDatabaseObject(string paramName)
        {
            var dbObjectSuffixes = new[] { "table", "column", "schema", "database", "view", "field", "index", "proc", "procedure" };
            return dbObjectSuffixes.Any(suffix => paramName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) ||
                   dbObjectSuffixes.Any(suffix => paramName.Contains(suffix, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Checks if a string contains patterns that might indicate SQL injection in object names
        /// </summary>
        private bool ContainsDatabaseInjectionPatterns(string value)
        {
            var injectionPatterns = new[]
            {
                @"--",           // SQL comment
                @"\/\*|\*\/",    // Block comment
                @";",            // Statement terminator
                @"\[|\]",        // SQL Server brackets outside of expected positions
                @"\s+",          // Multiple spaces (shouldn't be in object names)
                @"'|""",         // Quotes
                @"\(|\)",        // Parentheses
                @"\+",           // Plus operator
                @"="             // Equals operator
            };
            
            return injectionPatterns.Any(pattern => Regex.IsMatch(value, pattern));
        }
    }
} 