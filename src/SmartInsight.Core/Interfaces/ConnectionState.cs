namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Represents the current state of a data source connection
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Connector is disconnected from the data source
    /// </summary>
    Disconnected,
    
    /// <summary>
    /// Connector is in the process of connecting to the data source
    /// </summary>
    Connecting,
    
    /// <summary>
    /// Connector is successfully connected to the data source
    /// </summary>
    Connected,
    
    /// <summary>
    /// Connector is in the process of disconnecting from the data source
    /// </summary>
    Disconnecting,
    
    /// <summary>
    /// Connector encountered an error in its connection state
    /// </summary>
    Error
} 