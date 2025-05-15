namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Event arguments for connector state changes
/// </summary>
public class ConnectorStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Connector ID
    /// </summary>
    public string ConnectorId { get; }
    
    /// <summary>
    /// Previous connector state
    /// </summary>
    public ConnectionState PreviousState { get; }
    
    /// <summary>
    /// Current connector state
    /// </summary>
    public ConnectionState CurrentState { get; }
    
    /// <summary>
    /// Timestamp of the state change
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Creates a new connector state changed event args
    /// </summary>
    /// <param name="connectorId">Connector ID</param>
    /// <param name="previousState">Previous state</param>
    /// <param name="currentState">Current state</param>
    public ConnectorStateChangedEventArgs(string connectorId, ConnectionState previousState, ConnectionState currentState)
    {
        ConnectorId = connectorId;
        PreviousState = previousState;
        CurrentState = currentState;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for connector errors
/// </summary>
public class ConnectorErrorEventArgs : EventArgs
{
    /// <summary>
    /// Connector ID
    /// </summary>
    public string ConnectorId { get; }
    
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// Error details
    /// </summary>
    public string? Details { get; }
    
    /// <summary>
    /// Exception that caused the error
    /// </summary>
    public Exception? Exception { get; }
    
    /// <summary>
    /// Timestamp of the error
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Operation that was being performed
    /// </summary>
    public string Operation { get; }
    
    /// <summary>
    /// Creates a new connector error event args
    /// </summary>
    /// <param name="connectorId">Connector ID</param>
    /// <param name="message">Error message</param>
    /// <param name="operation">Operation that was being performed</param>
    /// <param name="details">Error details</param>
    /// <param name="exception">Exception that caused the error</param>
    public ConnectorErrorEventArgs(
        string connectorId, 
        string message, 
        string operation, 
        string? details = null, 
        Exception? exception = null)
    {
        ConnectorId = connectorId;
        Message = message;
        Details = details;
        Exception = exception;
        Timestamp = DateTime.UtcNow;
        Operation = operation;
    }
}

/// <summary>
/// Event arguments for connector progress
/// </summary>
public class ConnectorProgressEventArgs : EventArgs
{
    /// <summary>
    /// Connector ID
    /// </summary>
    public string ConnectorId { get; }
    
    /// <summary>
    /// Current progress (0-100)
    /// </summary>
    public int Progress { get; }
    
    /// <summary>
    /// Status message
    /// </summary>
    public string? StatusMessage { get; }
    
    /// <summary>
    /// Current operation
    /// </summary>
    public string Operation { get; }
    
    /// <summary>
    /// Timestamp of the progress update
    /// </summary>
    public DateTime Timestamp { get; }
    
    /// <summary>
    /// Total items to process
    /// </summary>
    public long TotalItems { get; }
    
    /// <summary>
    /// Items processed so far
    /// </summary>
    public long ProcessedItems { get; }
    
    /// <summary>
    /// Creates a new connector progress event args
    /// </summary>
    /// <param name="connectorId">Connector ID</param>
    /// <param name="progress">Current progress (0-100)</param>
    /// <param name="operation">Current operation</param>
    /// <param name="statusMessage">Status message</param>
    /// <param name="totalItems">Total items to process</param>
    /// <param name="processedItems">Items processed so far</param>
    public ConnectorProgressEventArgs(
        string connectorId, 
        int progress, 
        string operation, 
        string? statusMessage = null,
        long totalItems = 0,
        long processedItems = 0)
    {
        ConnectorId = connectorId;
        Progress = Math.Clamp(progress, 0, 100);
        StatusMessage = statusMessage;
        Operation = operation;
        Timestamp = DateTime.UtcNow;
        TotalItems = totalItems;
        ProcessedItems = processedItems;
    }
} 