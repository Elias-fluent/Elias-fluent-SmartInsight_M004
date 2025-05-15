namespace SmartInsight.Core.Validation;

/// <summary>
/// Represents a validation error
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Name of the property that has the error
    /// </summary>
    public string PropertyName { get; }
    
    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; }
    
    /// <summary>
    /// Creates a new validation error
    /// </summary>
    /// <param name="propertyName">Name of the property that has the error</param>
    /// <param name="errorMessage">Error message</param>
    public ValidationError(string propertyName, string errorMessage)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }
} 