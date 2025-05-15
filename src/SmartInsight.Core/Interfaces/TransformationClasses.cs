namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Parameters for data transformation
/// </summary>
public class TransformationParameters
{
    /// <summary>
    /// Transformation rules to apply
    /// </summary>
    public IReadOnlyList<TransformationRule> Rules { get; }
    
    /// <summary>
    /// Whether to preserve original data alongside transformed data
    /// </summary>
    public bool PreserveOriginalData { get; }
    
    /// <summary>
    /// Whether to fail the entire transformation if any rule fails
    /// </summary>
    public bool FailOnError { get; }
    
    /// <summary>
    /// Additional transformation options
    /// </summary>
    public IDictionary<string, object>? Options { get; }
    
    /// <summary>
    /// Creates new transformation parameters
    /// </summary>
    /// <param name="rules">Transformation rules to apply</param>
    /// <param name="preserveOriginalData">Whether to preserve original data alongside transformed data</param>
    /// <param name="failOnError">Whether to fail the entire transformation if any rule fails</param>
    /// <param name="options">Additional transformation options</param>
    public TransformationParameters(
        IEnumerable<TransformationRule> rules,
        bool preserveOriginalData = false,
        bool failOnError = false,
        IDictionary<string, object>? options = null)
    {
        Rules = rules.ToList().AsReadOnly();
        PreserveOriginalData = preserveOriginalData;
        FailOnError = failOnError;
        Options = options;
    }
}

/// <summary>
/// Defines a data transformation rule
/// </summary>
public class TransformationRule
{
    /// <summary>
    /// Unique identifier for the rule
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Description of the rule
    /// </summary>
    public string? Description { get; }
    
    /// <summary>
    /// Type of transformation (e.g., 'map', 'filter', 'aggregate', 'join', 'custom')
    /// </summary>
    public string Type { get; }
    
    /// <summary>
    /// Source fields this rule applies to
    /// </summary>
    public IReadOnlyList<string> SourceFields { get; }
    
    /// <summary>
    /// Target field(s) where the result will be stored
    /// </summary>
    public IReadOnlyList<string> TargetFields { get; }
    
    /// <summary>
    /// Transformation expression or logic
    /// </summary>
    public string? Expression { get; }
    
    /// <summary>
    /// Parameters for the transformation
    /// </summary>
    public IDictionary<string, object>? Parameters { get; }
    
    /// <summary>
    /// Order in which to apply this rule (lower numbers are applied first)
    /// </summary>
    public int Order { get; }
    
    /// <summary>
    /// Condition that must be true for this rule to be applied
    /// </summary>
    public string? Condition { get; }
    
    /// <summary>
    /// Creates a new transformation rule
    /// </summary>
    /// <param name="id">Unique identifier for the rule</param>
    /// <param name="type">Type of transformation</param>
    /// <param name="sourceFields">Source fields this rule applies to</param>
    /// <param name="targetFields">Target field(s) where the result will be stored</param>
    /// <param name="description">Description of the rule</param>
    /// <param name="expression">Transformation expression or logic</param>
    /// <param name="parameters">Parameters for the transformation</param>
    /// <param name="order">Order in which to apply this rule</param>
    /// <param name="condition">Condition that must be true for this rule to be applied</param>
    public TransformationRule(
        string id,
        string type,
        IEnumerable<string> sourceFields,
        IEnumerable<string> targetFields,
        string? description = null,
        string? expression = null,
        IDictionary<string, object>? parameters = null,
        int order = 0,
        string? condition = null)
    {
        Id = id;
        Type = type;
        Description = description;
        SourceFields = sourceFields.ToList().AsReadOnly();
        TargetFields = targetFields.ToList().AsReadOnly();
        Expression = expression;
        Parameters = parameters;
        Order = order;
        Condition = condition;
    }
}

/// <summary>
/// Result of a data transformation operation
/// </summary>
public class TransformationResult
{
    /// <summary>
    /// Whether the transformation was successful
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// Transformed data
    /// </summary>
    public IReadOnlyList<IDictionary<string, object>>? TransformedData { get; }
    
    /// <summary>
    /// Error message if the transformation failed
    /// </summary>
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// Additional error details
    /// </summary>
    public string? ErrorDetails { get; }
    
    /// <summary>
    /// Time taken for the transformation (in milliseconds)
    /// </summary>
    public long ExecutionTimeMs { get; }
    
    /// <summary>
    /// Timestamp of the transformation
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Number of records that were successfully transformed
    /// </summary>
    public int SuccessCount { get; }
    
    /// <summary>
    /// Number of records that failed transformation
    /// </summary>
    public int FailureCount { get; }
    
    /// <summary>
    /// Detailed rule execution results
    /// </summary>
    public IReadOnlyList<RuleExecutionResult>? RuleResults { get; }
    
    /// <summary>
    /// Creates a new successful transformation result
    /// </summary>
    /// <param name="transformedData">Transformed data</param>
    /// <param name="executionTimeMs">Time taken for the transformation (in milliseconds)</param>
    /// <param name="successCount">Number of records that were successfully transformed</param>
    /// <param name="failureCount">Number of records that failed transformation</param>
    /// <param name="ruleResults">Detailed rule execution results</param>
    /// <returns>A successful transformation result</returns>
    public static TransformationResult Success(
        IEnumerable<IDictionary<string, object>> transformedData,
        long executionTimeMs,
        int successCount = 0,
        int failureCount = 0,
        IEnumerable<RuleExecutionResult>? ruleResults = null)
    {
        return new TransformationResult(
            true,
            transformedData.ToList(),
            null,
            null,
            executionTimeMs,
            successCount,
            failureCount,
            ruleResults);
    }
    
    /// <summary>
    /// Creates a new failed transformation result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorDetails">Additional error details</param>
    /// <param name="executionTimeMs">Time taken for the transformation attempt (in milliseconds)</param>
    /// <param name="successCount">Number of records that were successfully transformed before the failure</param>
    /// <param name="failureCount">Number of records that failed transformation</param>
    /// <param name="ruleResults">Detailed rule execution results</param>
    /// <returns>A failed transformation result</returns>
    public static TransformationResult Failure(
        string errorMessage,
        string? errorDetails = null,
        long executionTimeMs = 0,
        int successCount = 0,
        int failureCount = 0,
        IEnumerable<RuleExecutionResult>? ruleResults = null)
    {
        return new TransformationResult(
            false,
            null,
            errorMessage,
            errorDetails,
            executionTimeMs,
            successCount,
            failureCount,
            ruleResults);
    }
    
    /// <summary>
    /// Creates a new transformation result
    /// </summary>
    /// <param name="isSuccess">Whether the transformation was successful</param>
    /// <param name="transformedData">Transformed data</param>
    /// <param name="errorMessage">Error message if the transformation failed</param>
    /// <param name="errorDetails">Additional error details</param>
    /// <param name="executionTimeMs">Time taken for the transformation (in milliseconds)</param>
    /// <param name="successCount">Number of records that were successfully transformed</param>
    /// <param name="failureCount">Number of records that failed transformation</param>
    /// <param name="ruleResults">Detailed rule execution results</param>
    private TransformationResult(
        bool isSuccess,
        IEnumerable<IDictionary<string, object>>? transformedData,
        string? errorMessage,
        string? errorDetails,
        long executionTimeMs,
        int successCount,
        int failureCount,
        IEnumerable<RuleExecutionResult>? ruleResults)
    {
        IsSuccess = isSuccess;
        TransformedData = transformedData?.ToList().AsReadOnly();
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
        ExecutionTimeMs = executionTimeMs;
        Timestamp = DateTime.UtcNow;
        SuccessCount = successCount;
        FailureCount = failureCount;
        RuleResults = ruleResults?.ToList().AsReadOnly();
    }
}

/// <summary>
/// Result of executing a transformation rule
/// </summary>
public class RuleExecutionResult
{
    /// <summary>
    /// Rule ID
    /// </summary>
    public string RuleId { get; }
    
    /// <summary>
    /// Whether the rule execution was successful
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// Error message if the rule execution failed
    /// </summary>
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// Time taken for the rule execution (in milliseconds)
    /// </summary>
    public long ExecutionTimeMs { get; }
    
    /// <summary>
    /// Number of records the rule was applied to
    /// </summary>
    public int RecordCount { get; }
    
    /// <summary>
    /// Number of records that were successfully transformed by this rule
    /// </summary>
    public int SuccessCount { get; }
    
    /// <summary>
    /// Number of records that failed transformation by this rule
    /// </summary>
    public int FailureCount { get; }
    
    /// <summary>
    /// Creates a new rule execution result
    /// </summary>
    /// <param name="ruleId">Rule ID</param>
    /// <param name="isSuccess">Whether the rule execution was successful</param>
    /// <param name="executionTimeMs">Time taken for the rule execution (in milliseconds)</param>
    /// <param name="recordCount">Number of records the rule was applied to</param>
    /// <param name="successCount">Number of records that were successfully transformed by this rule</param>
    /// <param name="failureCount">Number of records that failed transformation by this rule</param>
    /// <param name="errorMessage">Error message if the rule execution failed</param>
    public RuleExecutionResult(
        string ruleId,
        bool isSuccess,
        long executionTimeMs,
        int recordCount,
        int successCount,
        int failureCount,
        string? errorMessage = null)
    {
        RuleId = ruleId;
        IsSuccess = isSuccess;
        ExecutionTimeMs = executionTimeMs;
        ErrorMessage = errorMessage;
        RecordCount = recordCount;
        SuccessCount = successCount;
        FailureCount = failureCount;
    }
} 