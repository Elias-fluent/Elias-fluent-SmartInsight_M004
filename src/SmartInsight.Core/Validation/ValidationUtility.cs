using System.Text.RegularExpressions;

namespace SmartInsight.Core.Validation;

/// <summary>
/// Utility class for common validation operations
/// </summary>
public static class ValidationUtility
{
    /// <summary>
    /// Validates an email address
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if the email is valid, otherwise false</returns>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
            
        // Simple regex for email validation
        var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return regex.IsMatch(email);
    }
    
    /// <summary>
    /// Validates a URL
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <returns>True if the URL is valid, otherwise false</returns>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
            
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
    
    /// <summary>
    /// Validates that a string is not null or empty
    /// </summary>
    /// <param name="value">String to validate</param>
    /// <returns>True if the string is not null or empty, otherwise false</returns>
    public static bool IsNotNullOrEmpty(string? value)
    {
        return !string.IsNullOrEmpty(value);
    }
    
    /// <summary>
    /// Validates that a string is not null, empty, or whitespace
    /// </summary>
    /// <param name="value">String to validate</param>
    /// <returns>True if the string is not null, empty, or whitespace, otherwise false</returns>
    public static bool IsNotNullOrWhitespace(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
    
    /// <summary>
    /// Validates that a value is within a specified range
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="value">Value to validate</param>
    /// <param name="min">Minimum value (inclusive)</param>
    /// <param name="max">Maximum value (inclusive)</param>
    /// <returns>True if the value is within the range, otherwise false</returns>
    public static bool IsInRange<T>(T value, T min, T max) where T : IComparable<T>
    {
        return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
    }
    
    /// <summary>
    /// Validates a password against a regex pattern
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <param name="pattern">Regex pattern to validate against</param>
    /// <returns>True if the password matches the pattern, otherwise false</returns>
    public static bool IsValidPassword(string password, string pattern)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(pattern))
            return false;
            
        var regex = new Regex(pattern);
        return regex.IsMatch(password);
    }
} 