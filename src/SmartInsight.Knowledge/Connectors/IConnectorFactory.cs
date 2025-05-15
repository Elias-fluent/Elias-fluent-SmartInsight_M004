using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Interface for the connector factory that creates connector instances
    /// </summary>
    public interface IConnectorFactory
    {
        /// <summary>
        /// Creates a new instance of a connector by ID
        /// </summary>
        /// <param name="connectorId">ID of the connector to create</param>
        /// <returns>A new connector instance</returns>
        /// <exception cref="ArgumentException">If the connector ID is invalid</exception>
        /// <exception cref="InvalidOperationException">If the connector cannot be created</exception>
        IDataSourceConnector CreateConnector(string connectorId);
        
        /// <summary>
        /// Creates a new instance of a connector by ID and initializes it with the provided configuration
        /// </summary>
        /// <param name="connectorId">ID of the connector to create</param>
        /// <param name="configuration">Configuration to initialize the connector with</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A new, initialized connector instance</returns>
        /// <exception cref="ArgumentException">If the connector ID is invalid</exception>
        /// <exception cref="InvalidOperationException">If the connector cannot be created or initialized</exception>
        Task<IDataSourceConnector> CreateAndInitializeConnectorAsync(
            string connectorId, 
            IConnectorConfiguration configuration, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a connector of a specific type
        /// </summary>
        /// <typeparam name="T">Type of connector to create, must implement IDataSourceConnector</typeparam>
        /// <returns>A new connector instance</returns>
        /// <exception cref="InvalidOperationException">If the connector cannot be created</exception>
        T CreateConnector<T>() where T : IDataSourceConnector;
        
        /// <summary>
        /// Gets all available connector IDs
        /// </summary>
        /// <returns>Collection of available connector IDs</returns>
        IEnumerable<string> GetAvailableConnectorIds();
        
        /// <summary>
        /// Gets available connectors by source type
        /// </summary>
        /// <param name="sourceType">Source type to filter by</param>
        /// <returns>Collection of connector IDs matching the source type</returns>
        IEnumerable<string> GetConnectorIdsBySourceType(string sourceType);
    }
} 