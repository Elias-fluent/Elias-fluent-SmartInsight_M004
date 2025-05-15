using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Interface for observers that track connector lifecycle events
/// </summary>
public interface IConnectorLifecycleObserver
{
    /// <summary>
    /// Called when a connector is registered
    /// </summary>
    /// <param name="connector">Connector that was registered</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task OnConnectorRegisteredAsync(IDataSourceConnector connector, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a connector is unregistered
    /// </summary>
    /// <param name="connectorId">ID of the connector that was unregistered</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task OnConnectorUnregisteredAsync(string connectorId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a connector's state changes
    /// </summary>
    /// <param name="args">State change event arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task OnConnectorStateChangedAsync(ConnectorStateChangedEventArgs args, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a connector encounters an error
    /// </summary>
    /// <param name="args">Error event arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task OnConnectorErrorAsync(ConnectorErrorEventArgs args, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when a connector reports progress
    /// </summary>
    /// <param name="args">Progress event arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task OnConnectorProgressAsync(ConnectorProgressEventArgs args, CancellationToken cancellationToken = default);
} 