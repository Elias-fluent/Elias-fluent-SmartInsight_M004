namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the validation was successful
    /// </summary>
    public bool IsValid { get; }
    
    /// <summary>
    /// Collection of validation errors if the validation failed
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }
    
    /// <summary>
    /// Warning messages that don't prevent validation from succeeding
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }
    
    /// <summary>
    /// Creates a new successful validation result
    /// </summary>
    /// <param name="warnings">Optional warning messages</param>
    /// <returns>A successful validation result</returns>
    public static ValidationResult Success(IEnumerable<string>? warnings = null)
    {
        return new ValidationResult(true, Array.Empty<ValidationError>(), warnings ?? Array.Empty<string>());
    }
    
    /// <summary>
    /// Creates a new failed validation result
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <param name="warnings">Optional warning messages</param>
    /// <returns>A failed validation result</returns>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors, IEnumerable<string>? warnings = null)
    {
        return new ValidationResult(false, errors.ToList(), warnings ?? Array.Empty<string>());
    }
    
    /// <summary>
    /// Creates a new failed validation result with a single error
    /// </summary>
    /// <param name="field">Field that failed validation</param>
    /// <param name="message">Error message</param>
    /// <param name="warnings">Optional warning messages</param>
    /// <returns>A failed validation result</returns>
    public static ValidationResult Failure(string field, string message, IEnumerable<string>? warnings = null)
    {
        var error = new ValidationError(field, message);
        return new ValidationResult(false, new[] { error }, warnings ?? Array.Empty<string>());
    }
    
    /// <summary>
    /// Creates a new validation result
    /// </summary>
    /// <param name="isValid">Whether the validation was successful</param>
    /// <param name="errors">Collection of validation errors</param>
    /// <param name="warnings">Warning messages</param>
    private ValidationResult(bool isValid, IEnumerable<ValidationError> errors, IEnumerable<string> warnings)
    {
        IsValid = isValid;
        Errors = errors.ToList().AsReadOnly();
        Warnings = warnings.ToList().AsReadOnly();
    }
}

/// <summary>
/// Represents a validation error for a specific field
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Field that failed validation
    /// </summary>
    public string Field { get; }
    
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// Creates a new validation error
    /// </summary>
    /// <param name="field">Field that failed validation</param>
    /// <param name="message">Error message</param>
    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }
} 