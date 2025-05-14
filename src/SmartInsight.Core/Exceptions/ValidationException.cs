using SmartInsight.Core.Validation;

namespace SmartInsight.Core.Exceptions;

/// <summary>
/// Exception for validation errors
/// </summary>
public class ValidationException : SmartInsightException
{
    /// <summary>
    /// Validation result containing validation errors
    /// </summary>
    public ValidationResult ValidationResult { get; }
    
    /// <summary>
    /// Creates a new validation exception with a validation result
    /// </summary>
    /// <param name="validationResult">Validation result containing errors</param>
    public ValidationException(ValidationResult validationResult) 
        : base("The provided data is invalid.", "VALIDATION_ERROR")
    {
        ValidationResult = validationResult;
    }
    
    /// <summary>
    /// Creates a new validation exception with a message and validation result
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="validationResult">Validation result containing errors</param>
    public ValidationException(string message, ValidationResult validationResult) 
        : base(message, "VALIDATION_ERROR")
    {
        ValidationResult = validationResult;
    }
    
    /// <summary>
    /// Returns a string with all validation errors
    /// </summary>
    /// <returns>Formatted validation error message</returns>
    public override string ToString()
    {
        var errorMessages = string.Join("; ", ValidationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
        return $"{Message}. Validation errors: {errorMessages}";
    }
} 