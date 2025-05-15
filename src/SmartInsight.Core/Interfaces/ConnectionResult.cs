namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Represents the result of a connection operation
/// </summary>
public class ConnectionResult
{
    /// <summary>
    /// Whether the connection was successful
    /// </summary>
    public bool IsSuccess { get; }
    
    /// <summary>
    /// Connection identifier (if successful)
    /// </summary>
    public string? ConnectionId { get; }
    
    /// <summary>
    /// Error message (if unsuccessful)
    /// </summary>
    public string? ErrorMessage { get; }
    
    /// <summary>
    /// Additional error details (if unsuccessful)
    /// </summary>
    public string? ErrorDetails { get; }
    
    /// <summary>
    /// Connection information
    /// </summary>
    public IDictionary<string, object>? ConnectionInfo { get; }
    
    /// <summary>
    /// Timestamp of the connection
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Creates a new successful connection result
    /// </summary>
    /// <param name="connectionId">Connection identifier</param>
    /// <param name="connectionInfo">Additional connection information</param>
    /// <returns>A successful connection result</returns>
    public static ConnectionResult Success(string connectionId, IDictionary<string, object>? connectionInfo = null)
    {
        return new ConnectionResult(true, connectionId, null, null, connectionInfo);
    }
    
    /// <summary>
    /// Creates a new failed connection result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorDetails">Additional error details</param>
    /// <returns>A failed connection result</returns>
    public static ConnectionResult Failure(string errorMessage, string? errorDetails = null)
    {
        return new ConnectionResult(false, null, errorMessage, errorDetails, null);
    }
    
    /// <summary>
    /// Creates a new connection result
    /// </summary>
    /// <param name="isSuccess">Whether the connection was successful</param>
    /// <param name="connectionId">Connection identifier (if successful)</param>
    /// <param name="errorMessage">Error message (if unsuccessful)</param>
    /// <param name="errorDetails">Additional error details (if unsuccessful)</param>
    /// <param name="connectionInfo">Additional connection information</param>
    private ConnectionResult(
        bool isSuccess, 
        string? connectionId, 
        string? errorMessage, 
        string? errorDetails, 
        IDictionary<string, object>? connectionInfo)
    {
        IsSuccess = isSuccess;
        ConnectionId = connectionId;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
        ConnectionInfo = connectionInfo;
        Timestamp = DateTime.UtcNow;
    }
} 