using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Factory for creating connector instances
    /// </summary>
    public class ConnectorFactory : IConnectorFactory
    {
        private readonly IConnectorRegistry _registry;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConnectorFactory> _logger;
        
        /// <summary>
        /// Creates a new connector factory
        /// </summary>
        /// <param name="registry">Connector registry to use</param>
        /// <param name="serviceProvider">Service provider for dependency injection</param>
        /// <param name="logger">Logger instance</param>
        public ConnectorFactory(
            IConnectorRegistry registry,
            IServiceProvider serviceProvider,
            ILogger<ConnectorFactory> logger)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <inheritdoc />
        public IDataSourceConnector CreateConnector(string connectorId)
        {
            if (string.IsNullOrWhiteSpace(connectorId))
                throw new ArgumentException("Connector ID cannot be null or empty", nameof(connectorId));
                
            var registration = _registry.GetConnector(connectorId);
            if (registration == null)
                throw new ArgumentException($"Connector with ID '{connectorId}' is not registered", nameof(connectorId));
                
            try
            {
                _logger.LogDebug("Creating connector instance: {ConnectorId} ({ConnectorType})",
                    connectorId, registration.ConnectorType.FullName);
                    
                IDataSourceConnector connector = registration.CreateInstance(_serviceProvider);
                
                _logger.LogInformation("Created connector instance: {ConnectorId}", connectorId);
                return connector;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create connector instance: {ConnectorId}", connectorId);
                throw new InvalidOperationException($"Failed to create connector instance: {connectorId}", ex);
            }
        }
        
        /// <inheritdoc />
        public async Task<IDataSourceConnector> CreateAndInitializeConnectorAsync(
            string connectorId,
            IConnectorConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
                
            // Create the connector instance
            var connector = CreateConnector(connectorId);
            
            try
            {
                _logger.LogDebug("Initializing connector: {ConnectorId}", connectorId);
                
                // Initialize the connector
                bool initialized = await connector.InitializeAsync(configuration, cancellationToken);
                if (!initialized)
                {
                    throw new InvalidOperationException($"Failed to initialize connector: {connectorId}");
                }
                
                _logger.LogInformation("Initialized connector: {ConnectorId}", connectorId);
                return connector;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize connector: {ConnectorId}", connectorId);
                
                // Clean up the connector instance
                try
                {
                    await connector.DisposeAsync(cancellationToken);
                }
                catch (Exception disposeEx)
                {
                    _logger.LogWarning(disposeEx, "Error disposing connector after initialization failure: {ConnectorId}", connectorId);
                }
                
                throw new InvalidOperationException($"Failed to initialize connector: {connectorId}", ex);
            }
        }
        
        /// <inheritdoc />
        public T CreateConnector<T>() where T : IDataSourceConnector
        {
            try
            {
                _logger.LogDebug("Creating connector instance of type: {ConnectorType}", typeof(T).FullName);
                
                // Try to resolve the connector directly from the service provider
                T? connector = _serviceProvider.GetService<T>();
                if (connector != null)
                {
                    _logger.LogInformation("Created connector instance of type: {ConnectorType}", typeof(T).FullName);
                    return connector;
                }
                
                // Try to create using registration if available
                var registration = _registry.GetRegisteredConnectors()
                    .FirstOrDefault(r => r.ConnectorType == typeof(T));
                    
                if (registration != null)
                {
                    var instance = registration.CreateInstance(_serviceProvider);
                    if (instance is T typedInstance)
                    {
                        _logger.LogInformation("Created connector instance of type: {ConnectorType}", typeof(T).FullName);
                        return typedInstance;
                    }
                }
                
                // Fall back to direct instantiation
                connector = ActivatorUtilities.CreateInstance<T>(_serviceProvider);
                if (connector != null)
                {
                    _logger.LogInformation("Created connector instance of type: {ConnectorType} using ActivatorUtilities", typeof(T).FullName);
                    return connector;
                }
                
                throw new InvalidOperationException($"Unable to create connector of type {typeof(T).FullName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create connector of type {ConnectorType}", typeof(T).FullName);
                throw new InvalidOperationException($"Failed to create connector of type {typeof(T).FullName}", ex);
            }
        }
        
        /// <inheritdoc />
        public IEnumerable<string> GetAvailableConnectorIds()
        {
            return _registry.GetRegisteredConnectors().Select(r => r.Id).ToList();
        }
        
        /// <inheritdoc />
        public IEnumerable<string> GetConnectorIdsBySourceType(string sourceType)
        {
            if (string.IsNullOrWhiteSpace(sourceType))
                throw new ArgumentException("Source type cannot be null or empty", nameof(sourceType));
                
            return _registry.GetConnectorsBySourceType(sourceType).Select(r => r.Id).ToList();
        }
    }
} 