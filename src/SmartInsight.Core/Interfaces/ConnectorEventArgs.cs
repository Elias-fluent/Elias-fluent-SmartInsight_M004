using System;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Event arguments for connector state changes
/// </summary>
public class ConnectorStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// ID of the connector
    /// </summary>
    public string ConnectorId { get; }
    
    /// <summary>
    /// Previous connection state
    /// </summary>
    public ConnectionState PreviousState { get; }
    
    /// <summary>
    /// Current connection state
    /// </summary>
    public ConnectionState CurrentState { get; }
    
    /// <summary>
    /// Creates a new instance of connector state changed event args
    /// </summary>
    /// <param name="connectorId">ID of the connector</param>
    /// <param name="previousState">Previous connection state</param>
    /// <param name="currentState">Current connection state</param>
    public ConnectorStateChangedEventArgs(string connectorId, ConnectionState previousState, ConnectionState currentState)
    {
        ConnectorId = connectorId;
        PreviousState = previousState;
        CurrentState = currentState;
    }
}

/// <summary>
/// Event arguments for connector errors
/// </summary>
public class ConnectorErrorEventArgs : EventArgs
{
    /// <summary>
    /// ID of the connector
    /// </summary>
    public string ConnectorId { get; }
    
    /// <summary>
    /// The operation being performed when the error occurred
    /// </summary>
    public string Operation { get; }
    
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// Optional details about the error
    /// </summary>
    public string? Details { get; }
    
    /// <summary>
    /// Optional exception that caused the error
    /// </summary>
    public Exception? Exception { get; }
    
    /// <summary>
    /// Creates a new instance of connector error event args
    /// </summary>
    /// <param name="connectorId">ID of the connector</param>
    /// <param name="operation">The operation being performed</param>
    /// <param name="message">Error message</param>
    /// <param name="details">Optional additional details</param>
    /// <param name="exception">Optional exception</param>
    public ConnectorErrorEventArgs(
        string connectorId, 
        string operation, 
        string message, 
        string? details = null, 
        Exception? exception = null)
    {
        ConnectorId = connectorId;
        Operation = operation;
        Message = message;
        Details = details;
        Exception = exception;
    }
}

/// <summary>
/// Event arguments for connector progress updates
/// </summary>
public class ConnectorProgressEventArgs : EventArgs
{
    /// <summary>
    /// ID of the operation
    /// </summary>
    public string OperationId { get; }
    
    /// <summary>
    /// Current progress value
    /// </summary>
    public int Current { get; }
    
    /// <summary>
    /// Total expected progress value
    /// </summary>
    public int Total { get; }
    
    /// <summary>
    /// Progress message
    /// </summary>
    public string Message { get; }
    
    /// <summary>
    /// Progress as a percentage between 0 and 100
    /// </summary>
    public double ProgressPercentage => Total > 0 ? (double)Current / Total * 100 : 0;
    
    /// <summary>
    /// Creates a new instance of connector progress event args
    /// </summary>
    /// <param name="operationId">ID of the operation</param>
    /// <param name="current">Current progress value</param>
    /// <param name="total">Total expected progress value</param>
    /// <param name="message">Progress message</param>
    public ConnectorProgressEventArgs(string operationId, int current, int total, string message)
    {
        OperationId = operationId;
        Current = current;
        Total = total;
        Message = message;
    }
} 