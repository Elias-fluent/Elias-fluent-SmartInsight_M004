using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Registry for managing available connector implementations
/// </summary>
public class ConnectorRegistry
{
    private readonly ConcurrentDictionary<string, ConnectorRegistration> _connectors = new();
    private readonly ILogger<ConnectorRegistry> _logger;
    
    /// <summary>
    /// Creates a new connector registry
    /// </summary>
    /// <param name="logger">Logger</param>
    public ConnectorRegistry(ILogger<ConnectorRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Registers a connector type
    /// </summary>
    /// <param name="connectorType">Type of connector to register</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if registration was successful, false if already registered</returns>
    public async Task<bool> RegisterConnectorTypeAsync(Type connectorType, CancellationToken cancellationToken = default)
    {
        if (connectorType == null)
            throw new ArgumentNullException(nameof(connectorType));
            
        if (!typeof(IDataSourceConnector).IsAssignableFrom(connectorType))
            throw new ArgumentException($"Type {connectorType.FullName} does not implement IDataSourceConnector", nameof(connectorType));
            
        // Get connector info from type
        var connectorInfo = await GetConnectorInfoFromTypeAsync(connectorType, cancellationToken);
        
        // Create registration
        var registration = new ConnectorRegistration(
            connectorInfo.Id,
            connectorInfo.Name,
            connectorInfo.SourceType,
            connectorType,
            connectorInfo.Metadata);
            
        // Add to dictionary
        if (!_connectors.TryAdd(connectorInfo.Id, registration))
        {
            _logger.LogWarning("Connector with ID {ConnectorId} is already registered", connectorInfo.Id);
            return false;
        }
        
        _logger.LogInformation("Registered connector: {ConnectorId} ({Name}, {Type})",
            connectorInfo.Id, connectorInfo.Name, connectorInfo.SourceType);
            
        return true;
    }
    
    /// <summary>
    /// Unregisters a connector type
    /// </summary>
    /// <param name="connectorId">ID of the connector to unregister</param>
    /// <returns>True if unregistration was successful, false if not found</returns>
    public bool UnregisterConnectorType(string connectorId)
    {
        if (string.IsNullOrWhiteSpace(connectorId))
            throw new ArgumentException("Connector ID cannot be empty", nameof(connectorId));
            
        if (!_connectors.TryRemove(connectorId, out var registration))
            return false;
            
        _logger.LogInformation("Unregistered connector: {ConnectorId} ({Name}, {Type})",
            registration.Id, registration.Name, registration.SourceType);
            
        return true;
    }
    
    /// <summary>
    /// Gets a connector registration by ID
    /// </summary>
    /// <param name="connectorId">ID of the connector to get</param>
    /// <returns>Connector registration, or null if not found</returns>
    public ConnectorRegistration? GetConnectorRegistration(string connectorId)
    {
        if (string.IsNullOrWhiteSpace(connectorId))
            throw new ArgumentException("Connector ID cannot be empty", nameof(connectorId));
            
        return _connectors.TryGetValue(connectorId, out var registration) ? registration : null;
    }
    
    /// <summary>
    /// Gets all registered connector types
    /// </summary>
    /// <returns>Collection of connector registrations</returns>
    public IEnumerable<ConnectorRegistration> GetAllRegistrations()
    {
        return _connectors.Values.ToList();
    }
    
    /// <summary>
    /// Gets registrations matching a filter predicate
    /// </summary>
    /// <param name="filter">Filter predicate</param>
    /// <returns>Filtered collection of connector registrations</returns>
    public IEnumerable<ConnectorRegistration> GetRegistrations(Func<ConnectorRegistration, bool> filter)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));
            
        return _connectors.Values.Where(filter).ToList();
    }
    
    /// <summary>
    /// Gets registrations by source type
    /// </summary>
    /// <param name="sourceType">Source type to filter by</param>
    /// <returns>Filtered collection of connector registrations</returns>
    public IEnumerable<ConnectorRegistration> GetRegistrationsBySourceType(string sourceType)
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new ArgumentException("Source type cannot be empty", nameof(sourceType));
            
        return GetRegistrations(r => string.Equals(r.SourceType, sourceType, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Gets information about a connector from its type
    /// </summary>
    /// <param name="connectorType">Connector type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connector information</returns>
    private async Task<ConnectorInfo> GetConnectorInfoFromTypeAsync(Type connectorType, CancellationToken cancellationToken = default)
    {
        // Try creating a temporary instance to get connector information
        // This works if the connector has a parameterless constructor
        try
        {
            if (Activator.CreateInstance(connectorType) is IDataSourceConnector instance)
            {
                return new ConnectorInfo(
                    instance.Id,
                    instance.Name,
                    instance.SourceType,
                    instance.GetMetadata() ?? new Dictionary<string, object>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Could not create instance of {ConnectorType}: {Message}", 
                connectorType.FullName, ex.Message);
        }
        
        // Check for connector attributes as a fallback
        var attributes = connectorType.GetCustomAttributes(typeof(ConnectorMetadataAttribute), true);
        if (attributes.Length > 0 && attributes[0] is ConnectorMetadataAttribute metadataAttr)
        {
            var metadata = new Dictionary<string, object>
            {
                { "Description", metadataAttr.Description ?? string.Empty },
                { "Version", metadataAttr.Version ?? "1.0.0" }
            };
            
            if (metadataAttr.Capabilities?.Length > 0)
            {
                metadata["Capabilities"] = metadataAttr.Capabilities;
            }
            
            return new ConnectorInfo(
                metadataAttr.Id,
                metadataAttr.Name,
                metadataAttr.SourceType,
                metadata);
        }
        
        // As a final fallback, generate a default ID and name based on the type
        var id = connectorType.Name.Replace("Connector", string.Empty).ToLowerInvariant();
        var name = connectorType.Name.Replace("Connector", " Connector");
        var sourceType = "unknown";
        
        return new ConnectorInfo(id, name, sourceType, new Dictionary<string, object>());
    }
    
    /// <summary>
    /// Private helper class for connector information
    /// </summary>
    private class ConnectorInfo
    {
        public string Id { get; }
        public string Name { get; }
        public string SourceType { get; }
        public IDictionary<string, object> Metadata { get; }
        
        public ConnectorInfo(string id, string name, string sourceType, IDictionary<string, object> metadata)
        {
            Id = id;
            Name = name;
            SourceType = sourceType;
            Metadata = metadata;
        }
    }
} 