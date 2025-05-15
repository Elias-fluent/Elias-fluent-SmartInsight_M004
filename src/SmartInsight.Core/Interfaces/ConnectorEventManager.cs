using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Manages events for data source connectors
/// </summary>
public class ConnectorEventManager : IDisposable
{
    private readonly ConcurrentDictionary<string, IDataSourceConnector> _connectors = new();
    private readonly ConcurrentDictionary<string, ConnectorEventSubscription> _subscriptions = new();
    private readonly List<IConnectorLifecycleObserver> _observers = new();
    private readonly object _observersLock = new();
    private bool _isDisposed;

    /// <summary>
    /// Event raised when a connector's state changes
    /// </summary>
    public event EventHandler<ConnectorStateChangedEventArgs>? ConnectorStateChanged;
    
    /// <summary>
    /// Event raised when a connector encounters an error
    /// </summary>
    public event EventHandler<ConnectorErrorEventArgs>? ConnectorErrorOccurred;
    
    /// <summary>
    /// Event raised when a connector's extraction progress changes
    /// </summary>
    public event EventHandler<ConnectorProgressEventArgs>? ConnectorProgressChanged;
    
    /// <summary>
    /// Adds a lifecycle observer
    /// </summary>
    /// <param name="observer">Observer to add</param>
    public void AddObserver(IConnectorLifecycleObserver observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));
            
        lock (_observersLock)
        {
            _observers.Add(observer);
        }
    }
    
    /// <summary>
    /// Removes a lifecycle observer
    /// </summary>
    /// <param name="observer">Observer to remove</param>
    /// <returns>True if the observer was removed, false if it wasn't found</returns>
    public bool RemoveObserver(IConnectorLifecycleObserver observer)
    {
        if (observer == null)
            throw new ArgumentNullException(nameof(observer));
            
        lock (_observersLock)
        {
            return _observers.Remove(observer);
        }
    }
    
    /// <summary>
    /// Registers a connector with the event manager
    /// </summary>
    /// <param name="connector">Connector to register</param>
    /// <returns>True if registration is successful, false if already registered</returns>
    public bool RegisterConnector(IDataSourceConnector connector)
    {
        if (connector == null)
            throw new ArgumentNullException(nameof(connector));
            
        if (string.IsNullOrWhiteSpace(connector.Id))
            throw new ArgumentException("Connector ID cannot be empty", nameof(connector));
            
        // Add to connectors dictionary
        if (!_connectors.TryAdd(connector.Id, connector))
            return false;
            
        // Create subscription
        var subscription = new ConnectorEventSubscription(connector);
        
        // Hook up events
        subscription.ConnectorStateChanged += OnConnectorStateChanged;
        subscription.ConnectorErrorOccurred += OnConnectorErrorOccurred;
        subscription.ConnectorProgressChanged += OnConnectorProgressChanged;
        
        // Store subscription
        _subscriptions.TryAdd(connector.Id, subscription);
        
        // Notify observers
        NotifyObserversConnectorRegisteredAsync(connector).ConfigureAwait(false);
        
        return true;
    }
    
    /// <summary>
    /// Registers a connector with the event manager
    /// </summary>
    /// <param name="connector">Connector to register</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if registration is successful, false if already registered</returns>
    public async Task<bool> RegisterConnectorAsync(IDataSourceConnector connector, CancellationToken cancellationToken = default)
    {
        if (connector == null)
            throw new ArgumentNullException(nameof(connector));
            
        if (string.IsNullOrWhiteSpace(connector.Id))
            throw new ArgumentException("Connector ID cannot be empty", nameof(connector));
            
        // Add to connectors dictionary
        if (!_connectors.TryAdd(connector.Id, connector))
            return false;
            
        // Create subscription
        var subscription = new ConnectorEventSubscription(connector);
        
        // Hook up events
        subscription.ConnectorStateChanged += OnConnectorStateChanged;
        subscription.ConnectorErrorOccurred += OnConnectorErrorOccurred;
        subscription.ConnectorProgressChanged += OnConnectorProgressChanged;
        
        // Store subscription
        _subscriptions.TryAdd(connector.Id, subscription);
        
        // Notify observers
        await NotifyObserversConnectorRegisteredAsync(connector, cancellationToken);
        
        return true;
    }
    
    /// <summary>
    /// Unregisters a connector from the event manager
    /// </summary>
    /// <param name="connectorId">ID of the connector to unregister</param>
    /// <returns>True if unregistration is successful, false if not found</returns>
    public bool UnregisterConnector(string connectorId)
    {
        if (string.IsNullOrWhiteSpace(connectorId))
            throw new ArgumentException("Connector ID cannot be empty", nameof(connectorId));
            
        // Remove subscription first to prevent events
        if (_subscriptions.TryRemove(connectorId, out var subscription))
        {
            subscription.Dispose();
        }
        
        // Remove connector
        var result = _connectors.TryRemove(connectorId, out _);
        
        // Notify observers
        if (result)
        {
            NotifyObserversConnectorUnregisteredAsync(connectorId).ConfigureAwait(false);
        }
        
        return result;
    }
    
    /// <summary>
    /// Unregisters a connector from the event manager
    /// </summary>
    /// <param name="connectorId">ID of the connector to unregister</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unregistration is successful, false if not found</returns>
    public async Task<bool> UnregisterConnectorAsync(string connectorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectorId))
            throw new ArgumentException("Connector ID cannot be empty", nameof(connectorId));
            
        // Remove subscription first to prevent events
        if (_subscriptions.TryRemove(connectorId, out var subscription))
        {
            subscription.Dispose();
        }
        
        // Remove connector
        var result = _connectors.TryRemove(connectorId, out _);
        
        // Notify observers
        if (result)
        {
            await NotifyObserversConnectorUnregisteredAsync(connectorId, cancellationToken);
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets a registered connector by ID
    /// </summary>
    /// <param name="connectorId">ID of the connector to get</param>
    /// <returns>The connector, or null if not found</returns>
    public IDataSourceConnector? GetConnector(string connectorId)
    {
        if (string.IsNullOrWhiteSpace(connectorId))
            throw new ArgumentException("Connector ID cannot be empty", nameof(connectorId));
            
        return _connectors.TryGetValue(connectorId, out var connector) ? connector : null;
    }
    
    /// <summary>
    /// Gets all registered connectors
    /// </summary>
    /// <returns>Collection of connectors</returns>
    public IEnumerable<IDataSourceConnector> GetAllConnectors()
    {
        return _connectors.Values;
    }
    
    /// <summary>
    /// Gets connectors matching a filter predicate
    /// </summary>
    /// <param name="filter">Filter predicate</param>
    /// <returns>Filtered collection of connectors</returns>
    public IEnumerable<IDataSourceConnector> GetConnectors(Func<IDataSourceConnector, bool> filter)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));
            
        foreach (var connector in _connectors.Values)
        {
            if (filter(connector))
                yield return connector;
        }
    }
    
    /// <summary>
    /// Initializes a connector with the provided configuration
    /// </summary>
    /// <param name="connectorId">ID of the connector to initialize</param>
    /// <param name="configuration">Configuration to use</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>True if initialization is successful</returns>
    public async Task<bool> InitializeConnectorAsync(
        string connectorId, 
        IConnectorConfiguration configuration, 
        CancellationToken cancellationToken = default)
    {
        var connector = GetConnector(connectorId);
        if (connector == null)
            throw new KeyNotFoundException($"Connector with ID '{connectorId}' not found");
            
        try
        {
            return await connector.InitializeAsync(configuration, cancellationToken);
        }
        catch (Exception ex)
        {
            // Invoke error event directly since connector might not have had a chance
            var args = new ConnectorErrorEventArgs(
                connectorId,
                $"Failed to initialize connector: {ex.Message}",
                "Initialize",
                ex.ToString(),
                ex);
                
            OnConnectorErrorOccurred(this, args);
            
            return false;
        }
    }
    
    /// <summary>
    /// Connects a connector with the provided parameters
    /// </summary>
    /// <param name="connectorId">ID of the connector to connect</param>
    /// <param name="connectionParams">Connection parameters</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Connection result</returns>
    public async Task<ConnectionResult> ConnectAsync(
        string connectorId,
        IDictionary<string, string> connectionParams,
        CancellationToken cancellationToken = default)
    {
        var connector = GetConnector(connectorId);
        if (connector == null)
            return ConnectionResult.Failure($"Connector with ID '{connectorId}' not found");
            
        try
        {
            return await connector.ConnectAsync(connectionParams, cancellationToken);
        }
        catch (Exception ex)
        {
            // Invoke error event directly
            var args = new ConnectorErrorEventArgs(
                connectorId,
                $"Failed to connect connector: {ex.Message}",
                "Connect",
                ex.ToString(),
                ex);
                
            OnConnectorErrorOccurred(this, args);
            
            return ConnectionResult.Failure(ex.Message, ex.ToString());
        }
    }
    
    /// <summary>
    /// Disconnects a connector
    /// </summary>
    /// <param name="connectorId">ID of the connector to disconnect</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>True if disconnection is successful</returns>
    public async Task<bool> DisconnectAsync(
        string connectorId,
        CancellationToken cancellationToken = default)
    {
        var connector = GetConnector(connectorId);
        if (connector == null)
            return false;
            
        try
        {
            return await connector.DisconnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Invoke error event directly
            var args = new ConnectorErrorEventArgs(
                connectorId,
                $"Failed to disconnect connector: {ex.Message}",
                "Disconnect",
                ex.ToString(),
                ex);
                
            OnConnectorErrorOccurred(this, args);
            
            return false;
        }
    }
    
    /// <summary>
    /// Releases all connectors
    /// </summary>
    public async Task ReleaseAllConnectorsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var connector in _connectors.Values)
        {
            try
            {
                await connector.DisconnectAsync(cancellationToken);
                await connector.DisposeAsync(cancellationToken);
            }
            catch
            {
                // Ignore errors during shutdown
            }
        }
        
        // Clear collections
        _subscriptions.Clear();
        _connectors.Clear();
    }
    
    /// <summary>
    /// Event handler for connector state changes
    /// </summary>
    private void OnConnectorStateChanged(object? sender, ConnectorStateChangedEventArgs e)
    {
        // Notify subscribers
        ConnectorStateChanged?.Invoke(this, e);
        
        // Notify observers
        NotifyObserversConnectorStateChangedAsync(e).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Event handler for connector errors
    /// </summary>
    private void OnConnectorErrorOccurred(object? sender, ConnectorErrorEventArgs e)
    {
        // Notify subscribers
        ConnectorErrorOccurred?.Invoke(this, e);
        
        // Notify observers
        NotifyObserversConnectorErrorAsync(e).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Event handler for connector progress updates
    /// </summary>
    private void OnConnectorProgressChanged(object? sender, ConnectorProgressEventArgs e)
    {
        // Notify subscribers
        ConnectorProgressChanged?.Invoke(this, e);
        
        // Notify observers
        NotifyObserversConnectorProgressAsync(e).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Notifies observers that a connector was registered
    /// </summary>
    private async Task NotifyObserversConnectorRegisteredAsync(IDataSourceConnector connector, CancellationToken cancellationToken = default)
    {
        List<IConnectorLifecycleObserver> observers;
        
        lock (_observersLock)
        {
            observers = new List<IConnectorLifecycleObserver>(_observers);
        }
        
        foreach (var observer in observers)
        {
            try
            {
                await observer.OnConnectorRegisteredAsync(connector, cancellationToken);
            }
            catch (Exception)
            {
                // Ignore observer exceptions
            }
        }
    }
    
    /// <summary>
    /// Notifies observers that a connector was unregistered
    /// </summary>
    private async Task NotifyObserversConnectorUnregisteredAsync(string connectorId, CancellationToken cancellationToken = default)
    {
        List<IConnectorLifecycleObserver> observers;
        
        lock (_observersLock)
        {
            observers = new List<IConnectorLifecycleObserver>(_observers);
        }
        
        foreach (var observer in observers)
        {
            try
            {
                await observer.OnConnectorUnregisteredAsync(connectorId, cancellationToken);
            }
            catch (Exception)
            {
                // Ignore observer exceptions
            }
        }
    }
    
    /// <summary>
    /// Notifies observers that a connector's state changed
    /// </summary>
    private async Task NotifyObserversConnectorStateChangedAsync(ConnectorStateChangedEventArgs args, CancellationToken cancellationToken = default)
    {
        List<IConnectorLifecycleObserver> observers;
        
        lock (_observersLock)
        {
            observers = new List<IConnectorLifecycleObserver>(_observers);
        }
        
        foreach (var observer in observers)
        {
            try
            {
                await observer.OnConnectorStateChangedAsync(args, cancellationToken);
            }
            catch (Exception)
            {
                // Ignore observer exceptions
            }
        }
    }
    
    /// <summary>
    /// Notifies observers that a connector encountered an error
    /// </summary>
    private async Task NotifyObserversConnectorErrorAsync(ConnectorErrorEventArgs args, CancellationToken cancellationToken = default)
    {
        List<IConnectorLifecycleObserver> observers;
        
        lock (_observersLock)
        {
            observers = new List<IConnectorLifecycleObserver>(_observers);
        }
        
        foreach (var observer in observers)
        {
            try
            {
                await observer.OnConnectorErrorAsync(args, cancellationToken);
            }
            catch (Exception)
            {
                // Ignore observer exceptions
            }
        }
    }
    
    /// <summary>
    /// Notifies observers that a connector reported progress
    /// </summary>
    private async Task NotifyObserversConnectorProgressAsync(ConnectorProgressEventArgs args, CancellationToken cancellationToken = default)
    {
        List<IConnectorLifecycleObserver> observers;
        
        lock (_observersLock)
        {
            observers = new List<IConnectorLifecycleObserver>(_observers);
        }
        
        foreach (var observer in observers)
        {
            try
            {
                await observer.OnConnectorProgressAsync(args, cancellationToken);
            }
            catch (Exception)
            {
                // Ignore observer exceptions
            }
        }
    }
    
    /// <summary>
    /// Disposes the event manager and all subscriptions
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Disposes the event manager and all subscriptions
    /// </summary>
    /// <param name="disposing">Whether this is being called from Dispose</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;
            
        if (disposing)
        {
            // Dispose managed resources
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Dispose();
            }
            
            _subscriptions.Clear();
            _connectors.Clear();
        }
        
        _isDisposed = true;
    }
    
    /// <summary>
    /// Internal class to manage event subscriptions for a connector
    /// </summary>
    private class ConnectorEventSubscription : IDisposable
    {
        private readonly IDataSourceConnector _connector;
        private bool _isDisposed;
        
        public event EventHandler<ConnectorStateChangedEventArgs>? ConnectorStateChanged;
        public event EventHandler<ConnectorErrorEventArgs>? ConnectorErrorOccurred;
        public event EventHandler<ConnectorProgressEventArgs>? ConnectorProgressChanged;
        
        public ConnectorEventSubscription(IDataSourceConnector connector)
        {
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
            
            // Subscribe to connector events
            _connector.StateChanged += OnConnectorStateChanged;
            _connector.ErrorOccurred += OnConnectorErrorOccurred;
            _connector.ProgressChanged += OnConnectorProgressChanged;
        }
        
        private void OnConnectorStateChanged(object? sender, ConnectorStateChangedEventArgs e)
        {
            ConnectorStateChanged?.Invoke(sender, e);
        }
        
        private void OnConnectorErrorOccurred(object? sender, ConnectorErrorEventArgs e)
        {
            ConnectorErrorOccurred?.Invoke(sender, e);
        }
        
        private void OnConnectorProgressChanged(object? sender, ConnectorProgressEventArgs e)
        {
            ConnectorProgressChanged?.Invoke(sender, e);
        }
        
        public void Dispose()
        {
            if (_isDisposed)
                return;
                
            // Unsubscribe from connector events
            _connector.StateChanged -= OnConnectorStateChanged;
            _connector.ErrorOccurred -= OnConnectorErrorOccurred;
            _connector.ProgressChanged -= OnConnectorProgressChanged;
            
            _isDisposed = true;
        }
    }
} 