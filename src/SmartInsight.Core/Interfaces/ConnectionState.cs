namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Represents the current state of a connector's connection
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Connector has not been initialized
    /// </summary>
    NotInitialized,
    
    /// <summary>
    /// Connector is initializing
    /// </summary>
    Initializing,
    
    /// <summary>
    /// Connector has been initialized but is not connected
    /// </summary>
    Initialized,
    
    /// <summary>
    /// Connector is connecting
    /// </summary>
    Connecting,
    
    /// <summary>
    /// Connector is connected
    /// </summary>
    Connected,
    
    /// <summary>
    /// Connector is disconnecting
    /// </summary>
    Disconnecting,
    
    /// <summary>
    /// Connector is extracting data
    /// </summary>
    Extracting,
    
    /// <summary>
    /// Connector is transforming data
    /// </summary>
    Transforming,
    
    /// <summary>
    /// Connector encountered an error
    /// </summary>
    Error,
    
    /// <summary>
    /// Connector has been disposed
    /// </summary>
    Disposed
} 