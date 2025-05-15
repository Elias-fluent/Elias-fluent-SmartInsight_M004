using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Base interface for all data source connectors
/// </summary>
public interface IDataSourceConnector
{
    /// <summary>
    /// Unique identifier for the connector
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Human-readable name of the connector
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Type of data source this connector supports
    /// </summary>
    string SourceType { get; }
    
    /// <summary>
    /// Description of the connector
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Version of the connector
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Current connection state
    /// </summary>
    ConnectionState ConnectionState { get; }
    
    /// <summary>
    /// Event raised when connector state changes
    /// </summary>
    event EventHandler<ConnectorStateChangedEventArgs>? StateChanged;
    
    /// <summary>
    /// Event raised when connector encounters an error
    /// </summary>
    event EventHandler<ConnectorErrorEventArgs>? ErrorOccurred;
    
    /// <summary>
    /// Event raised when connector data extraction progress changes
    /// </summary>
    event EventHandler<ConnectorProgressEventArgs>? ProgressChanged;
    
    /// <summary>
    /// Initializes the connector
    /// </summary>
    /// <param name="configuration">Connector configuration</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>True if initialization is successful</returns>
    Task<bool> InitializeAsync(IConnectorConfiguration configuration, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates the connection parameters
    /// </summary>
    /// <param name="connectionParams">Connection parameters as a dictionary</param>
    /// <returns>Validation result with details of any issues found</returns>
    Task<ValidationResult> ValidateConnectionAsync(IDictionary<string, string> connectionParams);
    
    /// <summary>
    /// Connects to the data source
    /// </summary>
    /// <param name="connectionParams">Connection parameters as a dictionary</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Connection result with details of any issues</returns>
    Task<ConnectionResult> ConnectAsync(IDictionary<string, string> connectionParams, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests the connection to the data source
    /// </summary>
    /// <param name="connectionParams">Connection parameters as a dictionary</param>
    /// <returns>True if the connection is successful, otherwise false</returns>
    Task<bool> TestConnectionAsync(IDictionary<string, string> connectionParams);
    
    /// <summary>
    /// Discovers available data structures in the connected data source
    /// </summary>
    /// <param name="filter">Optional filter criteria for discovery</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Collection of discovered data structures</returns>
    Task<IEnumerable<DataStructureInfo>> DiscoverDataStructuresAsync(
        IDictionary<string, object>? filter = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Extracts data from the source
    /// </summary>
    /// <param name="extractionParams">Parameters for extraction process</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Extracted data in a standardized format</returns>
    Task<ExtractionResult> ExtractDataAsync(
        ExtractionParameters extractionParams,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Transforms extracted data according to specified transformation rules
    /// </summary>
    /// <param name="data">Data to transform</param>
    /// <param name="transformationParams">Transformation parameters</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Transformed data</returns>
    Task<TransformationResult> TransformDataAsync(
        IEnumerable<IDictionary<string, object>> data,
        TransformationParameters transformationParams,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disconnects from the data source and releases resources
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>True if disconnection is successful</returns>
    Task<bool> DisconnectAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Releases all resources used by the connector
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task DisposeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the capabilities of this connector
    /// </summary>
    /// <returns>Connector capabilities</returns>
    ConnectorCapabilities GetCapabilities();
    
    /// <summary>
    /// Gets metadata about this connector
    /// </summary>
    /// <returns>Connector metadata</returns>
    IDictionary<string, object> GetMetadata();
} 