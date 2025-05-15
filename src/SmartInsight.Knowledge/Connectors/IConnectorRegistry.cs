using System;
using System.Collections.Generic;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Interface for the connector registry that manages available connector implementations
    /// </summary>
    public interface IConnectorRegistry
    {
        /// <summary>
        /// Registers a connector type with the registry
        /// </summary>
        /// <param name="connectorType">Type of the connector to register</param>
        /// <returns>The registration information</returns>
        ConnectorRegistration RegisterConnector(Type connectorType);
        
        /// <summary>
        /// Registers a connector type with the registry
        /// </summary>
        /// <typeparam name="T">Type of the connector to register, must implement IDataSourceConnector</typeparam>
        /// <returns>The registration information</returns>
        ConnectorRegistration RegisterConnector<T>() where T : IDataSourceConnector;
        
        /// <summary>
        /// Gets a registered connector by its ID
        /// </summary>
        /// <param name="connectorId">ID of the connector to get</param>
        /// <returns>The connector registration if found, null otherwise</returns>
        ConnectorRegistration? GetConnector(string connectorId);
        
        /// <summary>
        /// Gets all registered connectors
        /// </summary>
        /// <returns>Collection of connector registrations</returns>
        IEnumerable<ConnectorRegistration> GetRegisteredConnectors();
        
        /// <summary>
        /// Gets all registered connectors of a specific source type
        /// </summary>
        /// <param name="sourceType">Source type to filter by</param>
        /// <returns>Collection of connector registrations matching the source type</returns>
        IEnumerable<ConnectorRegistration> GetConnectorsBySourceType(string sourceType);
        
        /// <summary>
        /// Unregisters a connector by its ID
        /// </summary>
        /// <param name="connectorId">ID of the connector to unregister</param>
        /// <returns>True if unregistered successfully, false otherwise</returns>
        bool UnregisterConnector(string connectorId);
        
        /// <summary>
        /// Gets the number of registered connectors
        /// </summary>
        int Count { get; }
    }
} 