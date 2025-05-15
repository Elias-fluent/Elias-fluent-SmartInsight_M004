using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;
using SmartInsight.AI.SQL.Interfaces;

namespace SmartInsight.AI.SQL.Models
{
    /// <summary>
    /// Represents a SQL template with metadata and parameter definitions
    /// </summary>
    public class SqlTemplate
    {
        /// <summary>
        /// Unique identifier for the template
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// Name of the template
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Description of the template
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// List of intent names that map to this template
        /// </summary>
        public List<string> IntentMapping { get; set; } = new List<string>();

        /// <summary>
        /// The SQL template string with parameter placeholders
        /// </summary>
        public string SqlTemplateText { get; set; } = null!;

        /// <summary>
        /// List of parameters required by this template
        /// </summary>
        public List<SqlTemplateParameter> Parameters { get; set; } = new List<SqlTemplateParameter>();

        /// <summary>
        /// Expected result type (can be a Core entity name or custom type)
        /// </summary>
        public string? ResultType { get; set; }

        /// <summary>
        /// Required permission to execute this template
        /// </summary>
        public string? PermissionRequired { get; set; }

        /// <summary>
        /// Template version for tracking changes
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// When the template was created
        /// </summary>
        public DateTime Created { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional last modified timestamp
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Tags for categorization and searching
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Whether this template allows operations without filtering (full table scan)
        /// </summary>
        public bool AllowFullTableScan { get; set; } = false;

        /// <summary>
        /// The database type this template is for
        /// </summary>
        public string DatabaseType { get; set; } = "SQLServer";

        /// <summary>
        /// Sample natural language queries that would use this template
        /// </summary>
        public List<string> SampleQueries { get; set; } = new List<string>();

        /// <summary>
        /// Keywords for template matching
        /// </summary>
        public List<string> Keywords { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a parameter definition for a SQL template
    /// </summary>
    public class SqlTemplateParameter
    {
        /// <summary>
        /// The parameter name (without @ prefix)
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The .NET type of the parameter
        /// </summary>
        public string Type { get; set; } = null!;

        /// <summary>
        /// Whether the parameter is required
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// Human-readable description of the parameter
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// Whether this is a system parameter (like tenantId) that should be automatically provided
        /// </summary>
        public bool IsSystemParameter { get; set; } = false;

        /// <summary>
        /// Default value for the parameter, if any
        /// </summary>
        public object? DefaultValue { get; set; }
        
        /// <summary>
        /// Minimum allowed value for numeric or date parameters
        /// </summary>
        public string? MinValue { get; set; }
        
        /// <summary>
        /// Maximum allowed value for numeric or date parameters
        /// </summary>
        public string? MaxValue { get; set; }
        
        /// <summary>
        /// List of allowed values for enum-like parameters
        /// </summary>
        public List<string>? AllowedValues { get; set; }
    }

    /// <summary>
    /// Represents an extracted parameter value from natural language
    /// </summary>
    public class ExtractedParameter
    {
        /// <summary>
        /// The parameter name (without @ prefix)
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The extracted value
        /// </summary>
        public object Value { get; set; } = null!;
        
        /// <summary>
        /// The .NET type of the parameter
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score of the extraction (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// The original text from which the parameter was extracted
        /// </summary>
        public string? OriginalText { get; set; }
    }

    /// <summary>
    /// Result of template selection
    /// </summary>
    public class TemplateSelectionResult
    {
        /// <summary>
        /// Whether the template selection was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Error message if selection failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The selected template, if any
        /// </summary>
        public SqlTemplate? SelectedTemplate { get; set; }

        /// <summary>
        /// Confidence score of the selection
        /// </summary>
        public double ConfidenceScore { get; set; }

        /// <summary>
        /// Alternative templates that could have been selected
        /// </summary>
        public List<SqlTemplate>? AlternativeTemplates { get; set; }
        
        /// <summary>
        /// Parameters extracted from the query for the selected template
        /// </summary>
        public Dictionary<string, ExtractedParameter> ExtractedParameters { get; set; } = new Dictionary<string, ExtractedParameter>();
        
        /// <summary>
        /// Context information about the query origin
        /// </summary>
        public string? QueryContext { get; set; }
    }

    /// <summary>
    /// Result of SQL generation
    /// </summary>
    public class SqlGenerationResult
    {
        /// <summary>
        /// Whether the generation was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Generated SQL
        /// </summary>
        public string Sql { get; set; } = null!;

        /// <summary>
        /// Parameters for the SQL
        /// </summary>
        public Dictionary<string, object>? Parameters { get; set; }

        /// <summary>
        /// Error message if generation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// ID of the template used for generation
        /// </summary>
        public string? TemplateId { get; set; }
    }

    /// <summary>
    /// Represents a validation issue found in SQL
    /// </summary>
    public class SqlValidationIssue
    {
        /// <summary>
        /// Description of the issue
        /// </summary>
        public string Description { get; set; } = null!;

        /// <summary>
        /// Category of the issue
        /// </summary>
        public ValidationCategory Category { get; set; }

        /// <summary>
        /// Severity of the issue
        /// </summary>
        public ValidationSeverity Severity { get; set; }

        /// <summary>
        /// Line number where the issue was found
        /// </summary>
        public int? LineNumber { get; set; }
        
        /// <summary>
        /// Character position in the line where the issue was found
        /// </summary>
        public int? Position { get; set; }

        /// <summary>
        /// Recommendation for fixing the issue
        /// </summary>
        public string? Recommendation { get; set; }
    }

    /// <summary>
    /// Categories of validation issues
    /// </summary>
    public enum ValidationCategory
    {
        /// <summary>
        /// Security-related issues
        /// </summary>
        Security,

        /// <summary>
        /// Performance-related issues
        /// </summary>
        Performance,

        /// <summary>
        /// Syntax-related issues
        /// </summary>
        Syntax,

        /// <summary>
        /// Semantic-related issues
        /// </summary>
        Semantic,

        /// <summary>
        /// Best practice violations
        /// </summary>
        BestPractice
    }

    /// <summary>
    /// Severity of a validation issue
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Information only
        /// </summary>
        Info,
        
        /// <summary>
        /// Warning that should be addressed
        /// </summary>
        Warning,
        
        /// <summary>
        /// Error that must be fixed
        /// </summary>
        Error,
        
        /// <summary>
        /// Critical issue that prevents execution
        /// </summary>
        Critical
    }

    /// <summary>
    /// Result of SQL execution
    /// </summary>
    public class SqlExecutionResult
    {
        /// <summary>
        /// Whether the execution was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Error message if execution failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Number of rows affected
        /// </summary>
        public int RowsAffected { get; set; }

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Result data (for SELECT queries)
        /// </summary>
        public object? Data { get; set; }
        
        /// <summary>
        /// Results in a standardized format for display and processing
        /// </summary>
        public object? Results { get; set; }

        /// <summary>
        /// Query that was executed (sanitized)
        /// </summary>
        public string ExecutedQuery { get; set; } = null!;
        
        /// <summary>
        /// The SQL that was generated before execution
        /// </summary>
        public string SqlGenerated { get; set; } = null!;
    }

    /// <summary>
    /// Represents a tenant context for SQL generation
    /// </summary>
    public class TenantContext
    {
        /// <summary>
        /// The tenant ID
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// The user ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// User permissions
        /// </summary>
        public List<string> Permissions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a log entry for SQL operations
    /// </summary>
    public class SqlLogEntry
    {
        /// <summary>
        /// Unique identifier for the log entry
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Timestamp of the log entry
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Type of operation being logged
        /// </summary>
        public SqlOperationType OperationType { get; set; }

        /// <summary>
        /// Original query that was received
        /// </summary>
        public string OriginalQuery { get; set; } = null!;

        /// <summary>
        /// ID of the template that was used
        /// </summary>
        public string? TemplateId { get; set; }

        /// <summary>
        /// Generated SQL (sanitized)
        /// </summary>
        public string? GeneratedSql { get; set; }

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long? ExecutionTimeMs { get; set; }

        /// <summary>
        /// Number of rows affected
        /// </summary>
        public int? RowsAffected { get; set; }

        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// User ID who performed the operation
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Tenant ID for the operation
        /// </summary>
        public Guid? TenantId { get; set; }
    }

    /// <summary>
    /// Types of SQL operations
    /// </summary>
    public enum SqlOperationType
    {
        /// <summary>
        /// SELECT operation
        /// </summary>
        Select,

        /// <summary>
        /// INSERT operation
        /// </summary>
        Insert,

        /// <summary>
        /// UPDATE operation
        /// </summary>
        Update,

        /// <summary>
        /// DELETE operation
        /// </summary>
        Delete,

        /// <summary>
        /// Other operation type
        /// </summary>
        Other,
        
        /// <summary>
        /// Unknown operation type
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Result of parameter validation
    /// </summary>
    public class ParameterValidationResult
    {
        /// <summary>
        /// Whether all required parameters are present and valid
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Parameters that are required but missing
        /// </summary>
        public List<string> MissingParameters { get; set; } = new List<string>();
        
        /// <summary>
        /// Parameters that are invalid with error message
        /// </summary>
        public Dictionary<string, string> InvalidParameters { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Parameters with low confidence scores
        /// </summary>
        public Dictionary<string, double> LowConfidenceParameters { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// Validation issues for parameters
        /// </summary>
        public List<ParameterValidationIssue> ValidationIssues { get; set; } = new List<ParameterValidationIssue>();
        
        /// <summary>
        /// Alias for ValidationIssues for backward compatibility
        /// </summary>
        public List<ParameterValidationIssue> Issues => ValidationIssues;
        
        /// <summary>
        /// Whether there are any security rule violations
        /// </summary>
        public bool HasSecurityViolations => ValidationIssues.Any(i => i.Severity == ValidationSeverity.Critical && 
                                                                i.RuleName.StartsWith("Security."));
        
        /// <summary>
        /// Whether there are any business rule violations
        /// </summary>
        public bool HasBusinessRuleViolations => ValidationIssues.Any(i => i.Severity == ValidationSeverity.Critical && 
                                                                   i.RuleName.StartsWith("Business."));
        
        /// <summary>
        /// Add a validation issue to this result
        /// </summary>
        /// <param name="issue">The validation issue to add</param>
        public void AddIssue(ParameterValidationIssue issue)
        {
            ValidationIssues.Add(issue);
            IsValid = IsValid && issue.Severity != ValidationSeverity.Critical;
            
            // Also track in the appropriate collection for backward compatibility
            if (issue.RuleName == "Required.Missing")
            {
                MissingParameters.Add(issue.ParameterName);
            }
            else if (issue.RuleName == "Type.Invalid")
            {
                InvalidParameters[issue.ParameterName] = issue.Description;
            }
            else if (issue.RuleName == "Confidence.Low")
            {
                if (issue.OriginalValue is double confidence)
                {
                    LowConfidenceParameters[issue.ParameterName] = confidence;
                }
            }
        }
        
        /// <summary>
        /// Get all issues for a specific parameter
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <returns>The list of validation issues for this parameter</returns>
        public IEnumerable<ParameterValidationIssue> GetIssuesForParameter(string parameterName)
        {
            return ValidationIssues.Where(i => i.ParameterName == parameterName);
        }
        
        /// <summary>
        /// Get all issues of a specific severity
        /// </summary>
        /// <param name="severity">The severity level to filter by</param>
        /// <returns>The list of validation issues with this severity</returns>
        public IEnumerable<ParameterValidationIssue> GetIssuesBySeverity(ValidationSeverity severity)
        {
            return ValidationIssues.Where(i => i.Severity == severity);
        }
    }

    /// <summary>
    /// Issue found during parameter validation
    /// </summary>
    public class ParameterValidationIssue
    {
        /// <summary>
        /// Name of the parameter with the issue
        /// </summary>
        public string ParameterName { get; set; } = null!;
        
        /// <summary>
        /// Name of the rule that found the issue
        /// </summary>
        public string RuleName { get; set; } = null!;
        
        /// <summary>
        /// Description of the issue
        /// </summary>
        public string Description { get; set; } = null!;
        
        /// <summary>
        /// Severity of the issue
        /// </summary>
        public ValidationSeverity Severity { get; set; }
        
        /// <summary>
        /// The original value that caused the issue
        /// </summary>
        public object? OriginalValue { get; set; }
        
        /// <summary>
        /// Recommendation for fixing the issue
        /// </summary>
        public string? Recommendation { get; set; }
        
        /// <summary>
        /// Issue description (for backward compatibility with tests)
        /// </summary>
        public string Issue => Description;
    }

    /// <summary>
    /// SQL log statistics
    /// </summary>
    public class SqlLogStatistics
    {
        /// <summary>
        /// Total number of queries
        /// </summary>
        public int TotalQueries { get; set; }

        /// <summary>
        /// Number of successful queries
        /// </summary>
        public int SuccessfulQueries { get; set; }

        /// <summary>
        /// Number of failed queries
        /// </summary>
        public int FailedQueries { get; set; }
        
        /// <summary>
        /// Average execution time in milliseconds
        /// </summary>
        public double? AverageExecutionTimeMs { get; set; } = 0.0;
        
        /// <summary>
        /// Minimum execution time in milliseconds
        /// </summary>
        public long? MinExecutionTimeMs { get; set; } = 0;
        
        /// <summary>
        /// Maximum execution time in milliseconds
        /// </summary>
        public long? MaxExecutionTimeMs { get; set; } = 0;
        
        /// <summary>
        /// Success rate (percentage of successful queries)
        /// </summary>
        public double? SuccessRate { get; set; } = 0.0;
        
        /// <summary>
        /// Count of operations by type
        /// </summary>
        public Dictionary<SqlOperationType, int>? OperationCounts { get; set; } = new Dictionary<SqlOperationType, int>();
        
        /// <summary>
        /// Most common error messages with counts
        /// </summary>
        public Dictionary<string, int>? ErrorCounts { get; set; } = new Dictionary<string, int>();
        
        /// <summary>
        /// Initialize a new instance of SqlLogStatistics
        /// </summary>
        public SqlLogStatistics()
        {
            // Calculate success rate whenever this class is created/initialized
            if (TotalQueries > 0)
            {
                SuccessRate = (double)SuccessfulQueries / TotalQueries * 100.0;
            }
            else
            {
                SuccessRate = 0.0;
            }
        }
    }

    /// <summary>
    /// Extension methods for SqlValidationResult
    /// </summary>
    public static class SqlValidationResultExtensions
    {
        /// <summary>
        /// Whether there are any critical security issues
        /// </summary>
        public static bool HasSecurityIssues(this SqlValidationResult result) => 
            result.Issues.Exists(i => i.Category == ValidationCategory.Security && i.Severity == ValidationSeverity.Critical);

        /// <summary>
        /// Whether there are any critical performance issues
        /// </summary>
        public static bool HasPerformanceIssues(this SqlValidationResult result) => 
            result.Issues.Exists(i => i.Category == ValidationCategory.Performance && i.Severity == ValidationSeverity.Critical);
    }
} 