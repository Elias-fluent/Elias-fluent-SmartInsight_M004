using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Provides access to connector instances with caching support
    /// </summary>
    public class ConnectorProvider
    {
        private readonly IConnectorFactory _factory;
        private readonly ILogger<ConnectorProvider> _logger;
        private readonly ConcurrentDictionary<string, IDataSourceConnector> _connectorCache = new();
        
        /// <summary>
        /// Creates a new connector provider
        /// </summary>
        /// <param name="factory">Connector factory to use</param>
        /// <param name="logger">Logger instance</param>
        public ConnectorProvider(IConnectorFactory factory, ILogger<ConnectorProvider> logger)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Gets a connector by ID, creating a new instance if not already cached
        /// </summary>
        /// <param name="connectorId">ID of the connector to get</param>
        /// <param name="useCache">Whether to use cached instances</param>
        /// <returns>The connector instance</returns>
        public IDataSourceConnector GetConnector(string connectorId, bool useCache = true)
        {
            if (string.IsNullOrWhiteSpace(connectorId))
                throw new ArgumentException("Connector ID cannot be null or empty", nameof(connectorId));
                
            if (useCache && _connectorCache.TryGetValue(connectorId, out var cachedConnector))
            {
                _logger.LogDebug("Returning cached connector instance: {ConnectorId}", connectorId);
                return cachedConnector;
            }
            
            var connector = _factory.CreateConnector(connectorId);
            if (useCache)
            {
                _connectorCache[connectorId] = connector;
            }
            
            return connector;
        }
        
        /// <summary>
        /// Gets a connector by ID and initializes it with the provided configuration
        /// </summary>
        /// <param name="connectorId">ID of the connector to get</param>
        /// <param name="configuration">Configuration to initialize the connector with</param>
        /// <param name="useCache">Whether to use cached instances</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The initialized connector instance</returns>
        public async Task<IDataSourceConnector> GetAndInitializeConnectorAsync(
            string connectorId,
            IConnectorConfiguration configuration,
            bool useCache = true,
            CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
                
            // Use existing cached connector if available
            if (useCache && _connectorCache.TryGetValue(connectorId, out var cachedConnector))
            {
                _logger.LogDebug("Initializing cached connector instance: {ConnectorId}", connectorId);
                
                bool success = await cachedConnector.InitializeAsync(configuration, cancellationToken);
                if (success)
                {
                    return cachedConnector;
                }
                
                // If initialization fails, remove from cache and try creating a new instance
                _connectorCache.TryRemove(connectorId, out _);
                _logger.LogWarning("Failed to initialize cached connector, creating new instance: {ConnectorId}", connectorId);
            }
            
            // Create and initialize a new connector
            var connector = await _factory.CreateAndInitializeConnectorAsync(connectorId, configuration, cancellationToken);
            
            // Cache the new connector if caching is enabled
            if (useCache)
            {
                _connectorCache[connectorId] = connector;
            }
            
            return connector;
        }
        
        /// <summary>
        /// Gets a connector of a specific type
        /// </summary>
        /// <typeparam name="T">Type of connector to get</typeparam>
        /// <param name="useCache">Whether to use cached instances</param>
        /// <returns>The connector instance</returns>
        public T GetConnector<T>(bool useCache = true) where T : IDataSourceConnector
        {
            string typeName = typeof(T).FullName ?? typeof(T).Name;
            
            // Check cache first if enabled
            if (useCache)
            {
                foreach (var cachedConnector in _connectorCache.Values)
                {
                    if (cachedConnector is T typedConnector)
                    {
                        _logger.LogDebug("Returning cached connector instance of type: {ConnectorType}", typeName);
                        return typedConnector;
                    }
                }
            }
            
            // Create a new instance
            var connector = _factory.CreateConnector<T>();
            
            // Cache by type name if caching is enabled
            if (useCache)
            {
                _connectorCache[typeName] = connector;
            }
            
            return connector;
        }
        
        /// <summary>
        /// Gets all available connector IDs from the factory
        /// </summary>
        /// <returns>Collection of available connector IDs</returns>
        public IEnumerable<string> GetAvailableConnectorIds()
        {
            return _factory.GetAvailableConnectorIds();
        }
        
        /// <summary>
        /// Clears the connector cache
        /// </summary>
        public void ClearCache()
        {
            _logger.LogInformation("Clearing connector cache. {CachedCount} connectors removed.", _connectorCache.Count);
            _connectorCache.Clear();
        }
        
        /// <summary>
        /// Removes a connector from the cache
        /// </summary>
        /// <param name="connectorId">ID of the connector to remove</param>
        /// <returns>True if the connector was found and removed, otherwise false</returns>
        public bool RemoveFromCache(string connectorId)
        {
            if (string.IsNullOrWhiteSpace(connectorId))
                return false;
                
            bool result = _connectorCache.TryRemove(connectorId, out _);
            if (result)
            {
                _logger.LogDebug("Removed connector from cache: {ConnectorId}", connectorId);
            }
            
            return result;
        }
    }
} 