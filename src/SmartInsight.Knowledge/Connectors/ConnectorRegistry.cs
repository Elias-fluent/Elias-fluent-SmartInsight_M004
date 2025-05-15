using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Implementation of the connector registry that manages available connector types
    /// </summary>
    public class ConnectorRegistry : IConnectorRegistry
    {
        private readonly ConcurrentDictionary<string, ConnectorRegistration> _connectors = new();
        private readonly ILogger<ConnectorRegistry> _logger;
        
        public ConnectorRegistry(ILogger<ConnectorRegistry> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <inheritdoc />
        public ConnectorRegistration RegisterConnector(Type connectorType)
        {
            if (connectorType == null)
                throw new ArgumentNullException(nameof(connectorType));
                
            if (!typeof(IDataSourceConnector).IsAssignableFrom(connectorType))
                throw new ArgumentException($"Type {connectorType.FullName} does not implement IDataSourceConnector", nameof(connectorType));
            
            try
            {
                // Get connector metadata from attributes
                var metadataAttr = connectorType.GetCustomAttribute<ConnectorMetadataAttribute>();
                if (metadataAttr == null)
                {
                    throw new InvalidOperationException(
                        $"Connector type {connectorType.FullName} does not have a ConnectorMetadataAttribute");
                }
                
                var registration = new ConnectorRegistration(
                    metadataAttr.Id,
                    metadataAttr.Name,
                    metadataAttr.SourceType,
                    connectorType);
                
                if (_connectors.TryGetValue(registration.Id, out var existing))
                {
                    _logger.LogWarning(
                        "Connector with ID {ConnectorId} is already registered (type: {ExistingType}). Replacing with {NewType}",
                        registration.Id, existing.ConnectorType.FullName, connectorType.FullName);
                }
                
                _connectors[registration.Id] = registration;
                _logger.LogInformation("Registered connector: {ConnectorId} ({ConnectorName})", 
                    registration.Id, registration.Name);
                
                return registration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register connector type {ConnectorType}", connectorType.FullName);
                throw;
            }
        }
        
        /// <inheritdoc />
        public ConnectorRegistration RegisterConnector<T>() where T : IDataSourceConnector
        {
            return RegisterConnector(typeof(T));
        }
        
        /// <inheritdoc />
        public ConnectorRegistration? GetConnector(string connectorId)
        {
            if (string.IsNullOrEmpty(connectorId))
                throw new ArgumentException("Connector ID cannot be null or empty", nameof(connectorId));
            
            return _connectors.TryGetValue(connectorId, out var registration) ? registration : null;
        }
        
        /// <inheritdoc />
        public IEnumerable<ConnectorRegistration> GetRegisteredConnectors()
        {
            return _connectors.Values.ToList();
        }
        
        /// <inheritdoc />
        public IEnumerable<ConnectorRegistration> GetConnectorsBySourceType(string sourceType)
        {
            if (string.IsNullOrEmpty(sourceType))
                throw new ArgumentException("Source type cannot be null or empty", nameof(sourceType));
            
            return _connectors.Values
                .Where(r => string.Equals(r.SourceType, sourceType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        /// <inheritdoc />
        public bool UnregisterConnector(string connectorId)
        {
            if (string.IsNullOrEmpty(connectorId))
                throw new ArgumentException("Connector ID cannot be null or empty", nameof(connectorId));
            
            var result = _connectors.TryRemove(connectorId, out _);
            if (result)
            {
                _logger.LogInformation("Unregistered connector: {ConnectorId}", connectorId);
            }
            
            return result;
        }
        
        /// <inheritdoc />
        public int Count => _connectors.Count;
        
        /// <summary>
        /// Discovers and registers all connector types in the specified assembly
        /// </summary>
        /// <param name="assembly">The assembly to scan for connector types</param>
        /// <returns>Number of connectors registered</returns>
        public int RegisterConnectorsFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            
            try
            {
                var connectorTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface)
                    .Where(t => typeof(IDataSourceConnector).IsAssignableFrom(t))
                    .Where(t => t.GetCustomAttribute<ConnectorMetadataAttribute>() != null)
                    .ToList();
                
                _logger.LogInformation("Found {ConnectorCount} connector types in assembly {AssemblyName}",
                    connectorTypes.Count, assembly.GetName().Name);
                
                int count = 0;
                foreach (var type in connectorTypes)
                {
                    try
                    {
                        RegisterConnector(type);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to register connector type {ConnectorType}", type.FullName);
                    }
                }
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning assembly {AssemblyName} for connector types", 
                    assembly.GetName().Name);
                throw;
            }
        }
        
        /// <summary>
        /// Discovers and registers all connector types in the entry assembly and its referenced assemblies
        /// </summary>
        /// <returns>Total number of connectors registered</returns>
        public int RegisterAllConnectors()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null)
            {
                _logger.LogWarning("Entry assembly could not be determined");
                return 0;
            }
            
            int totalRegistered = 0;
            var processedAssemblies = new HashSet<string>();
            
            // Register connectors from entry assembly
            totalRegistered += RegisterConnectorsFromAssembly(entryAssembly);
            processedAssemblies.Add(entryAssembly.FullName ?? string.Empty);
            
            // Register connectors from referenced assemblies
            foreach (var reference in entryAssembly.GetReferencedAssemblies())
            {
                try
                {
                    if (processedAssemblies.Contains(reference.FullName))
                        continue;
                    
                    var assembly = Assembly.Load(reference);
                    totalRegistered += RegisterConnectorsFromAssembly(assembly);
                    processedAssemblies.Add(reference.FullName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading referenced assembly {AssemblyName}", reference.FullName);
                }
            }
            
            _logger.LogInformation("Registered a total of {TotalConnectors} connectors", totalRegistered);
            return totalRegistered;
        }
    }
} 