namespace SmartInsight.Core.Validation;

/// <summary>
/// Generic interface for validators
/// </summary>
/// <typeparam name="T">Type to validate</typeparam>
public interface IValidator<T>
{
    /// <summary>
    /// Validates the specified entity
    /// </summary>
    /// <param name="entity">Entity to validate</param>
    /// <returns>Validation result containing success status and any validation errors</returns>
    ValidationResult Validate(T entity);
} 