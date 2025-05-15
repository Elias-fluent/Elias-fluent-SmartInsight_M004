using System;
using System.Collections.Generic;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Result of a connection operation
/// </summary>
public class ConnectionResult
{
    private readonly List<ValidationError> _errors = new();
    
    /// <summary>
    /// Whether the connection was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message if the connection failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Connection identifier
    /// </summary>
    public string? ConnectionId { get; set; }
    
    /// <summary>
    /// Server version or other version information
    /// </summary>
    public string? ServerVersion { get; set; }
    
    /// <summary>
    /// Additional information about the connection
    /// </summary>
    public IDictionary<string, object>? ConnectionInfo { get; set; }
    
    /// <summary>
    /// When the connection was created
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// Connection errors
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();
    
    /// <summary>
    /// Creates a new connection result
    /// </summary>
    public ConnectionResult()
    {
    }
    
    /// <summary>
    /// Adds an error to the connection result
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="errorMessage">Error message</param>
    public void AddError(string fieldName, string errorMessage)
    {
        _errors.Add(new ValidationError(fieldName, errorMessage));
    }
    
    /// <summary>
    /// Adds multiple errors to the connection result
    /// </summary>
    /// <param name="errors">Collection of validation errors</param>
    public void AddRange(IEnumerable<ValidationError> errors)
    {
        _errors.AddRange(errors);
    }
    
    /// <summary>
    /// Creates a successful connection result
    /// </summary>
    /// <param name="connectionId">Optional connection identifier</param>
    /// <param name="serverVersion">Optional server version information</param>
    /// <param name="connectionInfo">Optional additional connection information</param>
    /// <returns>A successful connection result</returns>
    public static ConnectionResult Success(
        string? connectionId = null,
        string? serverVersion = null,
        IDictionary<string, object>? connectionInfo = null)
    {
        return new ConnectionResult
        {
            IsSuccess = true,
            ConnectionId = connectionId,
            ServerVersion = serverVersion,
            ConnectionInfo = connectionInfo
        };
    }
    
    /// <summary>
    /// Creates a failed connection result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <returns>A failed connection result</returns>
    public static ConnectionResult Failure(string errorMessage)
    {
        return new ConnectionResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
    
    /// <summary>
    /// Creates a failed connection result with validation errors
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errors">Validation errors</param>
    /// <returns>A failed connection result</returns>
    public static ConnectionResult Failure(string errorMessage, IEnumerable<ValidationError> errors)
    {
        var result = new ConnectionResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
        
        result.AddRange(errors);
        return result;
    }
} 