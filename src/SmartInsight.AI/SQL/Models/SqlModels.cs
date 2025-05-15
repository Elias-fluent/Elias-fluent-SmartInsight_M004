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
        public string TemplateId { get; set; } = null!;

        /// <summary>
        /// Human-readable description of the template
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
        /// Confidence score of the extraction (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// The original text from which the parameter was extracted
        /// </summary>
        public string? OriginalText { get; set; }
    }

    /// <summary>
    /// Result of template selection process
    /// </summary>
    public class TemplateSelectionResult
    {
        /// <summary>
        /// The selected template, if any
        /// </summary>
        public SqlTemplate? SelectedTemplate { get; set; }

        /// <summary>
        /// Confidence score of the selection (0.0 to 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Whether a suitable template was found
        /// </summary>
        public bool TemplateFound => SelectedTemplate != null;

        /// <summary>
        /// Alternative templates that were considered
        /// </summary>
        public List<SqlTemplate> AlternativeTemplates { get; set; } = new List<SqlTemplate>();

        /// <summary>
        /// Explanation of the selection process
        /// </summary>
        public string? Explanation { get; set; }
    }

    /// <summary>
    /// Result of SQL generation process
    /// </summary>
    public class SqlGenerationResult
    {
        /// <summary>
        /// The generated SQL query
        /// </summary>
        public string Sql { get; set; } = null!;

        /// <summary>
        /// Parameters collection for the SQL query
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether the generation was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Error message if generation failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// The template that was used for generation
        /// </summary>
        public SqlTemplate? SourceTemplate { get; set; }
    }

    /// <summary>
    /// Result of SQL validation
    /// </summary>
    public class SqlValidationResult
    {
        /// <summary>
        /// Whether the SQL is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation issues found
        /// </summary>
        public List<SqlValidationIssue> Issues { get; set; } = new List<SqlValidationIssue>();

        /// <summary>
        /// Whether there are any critical security issues
        /// </summary>
        public bool HasSecurityIssues => Issues.Exists(i => i.Category == ValidationCategory.Security && i.Severity == ValidationSeverity.Critical);

        /// <summary>
        /// Whether there are any critical performance issues
        /// </summary>
        public bool HasPerformanceIssues => Issues.Exists(i => i.Category == ValidationCategory.Performance && i.Severity == ValidationSeverity.Critical);
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
    /// Severity levels for validation issues
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Informational issues
        /// </summary>
        Info,

        /// <summary>
        /// Warning issues
        /// </summary>
        Warning,

        /// <summary>
        /// Critical issues
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
        /// Query that was executed (sanitized)
        /// </summary>
        public string ExecutedQuery { get; set; } = null!;
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
        Other
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
        /// List of missing required parameters
        /// </summary>
        public List<string> MissingParameters { get; set; } = new List<string>();

        /// <summary>
        /// List of parameters with invalid values
        /// </summary>
        public Dictionary<string, string> InvalidParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// List of parameters with low confidence
        /// </summary>
        public Dictionary<string, double> LowConfidenceParameters { get; set; } = new Dictionary<string, double>();
        
        /// <summary>
        /// All validation issues found
        /// </summary>
        public List<ParameterValidationIssue> ValidationIssues { get; set; } = new List<ParameterValidationIssue>();
        
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
} 