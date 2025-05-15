namespace SmartInsight.Core.Validation;

/// <summary>
/// Result of a validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<ValidationError> Errors { get; } = new List<ValidationError>();

    /// <summary>
    /// Whether the validation succeeded
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Adds a validation error
    /// </summary>
    /// <param name="propertyName">Name of the property that has the error</param>
    /// <param name="errorMessage">Error message</param>
    public void AddError(string propertyName, string errorMessage)
    {
        Errors.Add(new ValidationError(propertyName, errorMessage));
    }

    /// <summary>
    /// Static method to create a successful validation result
    /// </summary>
    /// <returns>A valid validation result with no errors</returns>
    public static ValidationResult Success() => new ValidationResult();

    /// <summary>
    /// Static method to create a failed validation result with one error
    /// </summary>
    /// <param name="propertyName">Name of the property that has the error</param>
    /// <param name="errorMessage">Error message</param>
    /// <returns>An invalid validation result with one error</returns>
    public static ValidationResult Failure(string propertyName, string errorMessage)
    {
        var result = new ValidationResult();
        result.AddError(propertyName, errorMessage);
        return result;
    }
} 