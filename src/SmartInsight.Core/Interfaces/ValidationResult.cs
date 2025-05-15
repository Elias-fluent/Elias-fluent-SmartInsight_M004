using System.Collections.Generic;
using System.Linq;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class ValidationResult
{
    private readonly List<ValidationError> _errors = new();
    private readonly List<string> _warnings = new();
    
    /// <summary>
    /// True if validation passed with no errors
    /// </summary>
    public bool IsValid => !Errors.Any();
    
    /// <summary>
    /// Collection of validation errors
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();
    
    /// <summary>
    /// Warning messages that don't prevent validation from succeeding
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();
    
    /// <summary>
    /// Creates a new successful validation result
    /// </summary>
    /// <param name="warnings">Optional warning messages</param>
    /// <returns>A successful validation result</returns>
    public static ValidationResult Success(IEnumerable<string>? warnings = null)
    {
        var result = new ValidationResult();
        if (warnings != null)
        {
            result._warnings.AddRange(warnings);
        }
        return result;
    }
    
    /// <summary>
    /// Creates a new failed validation result
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <param name="warnings">Optional warning messages</param>
    /// <returns>A failed validation result</returns>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors, IEnumerable<string>? warnings = null)
    {
        var result = new ValidationResult();
        result._errors.AddRange(errors);
        if (warnings != null)
        {
            result._warnings.AddRange(warnings);
        }
        return result;
    }
    
    /// <summary>
    /// Creates a new failed validation result with a single error
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="warnings">Optional warning messages</param>
    /// <returns>A failed validation result</returns>
    public static ValidationResult Failure(string fieldName, string errorMessage, IEnumerable<string>? warnings = null)
    {
        var result = new ValidationResult();
        result.AddError(fieldName, errorMessage);
        if (warnings != null)
        {
            result._warnings.AddRange(warnings);
        }
        return result;
    }
    
    /// <summary>
    /// Creates a new validation result
    /// </summary>
    /// <param name="warnings">Warning messages</param>
    private ValidationResult()
    {
    }
    
    /// <summary>
    /// Adds an error to the validation result
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="errorMessage">Error message</param>
    public void AddError(string fieldName, string errorMessage)
    {
        _errors.Add(new ValidationError(fieldName, errorMessage));
    }
    
    /// <summary>
    /// Adds a warning message to the validation result
    /// </summary>
    /// <param name="warningMessage">Warning message</param>
    public void AddWarning(string warningMessage)
    {
        _warnings.Add(warningMessage);
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
    public string FieldName { get; }
    
    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; }
    
    /// <summary>
    /// Creates a new validation error
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="errorMessage">Error message</param>
    public ValidationError(string fieldName, string errorMessage)
    {
        FieldName = fieldName;
        ErrorMessage = errorMessage;
    }
} 