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
    /// Validates the connection parameters
    /// </summary>
    /// <param name="connectionParams">Connection parameters as a dictionary</param>
    /// <returns>True if the connection parameters are valid, otherwise false</returns>
    Task<bool> ValidateConnectionAsync(IDictionary<string, string> connectionParams);
    
    /// <summary>
    /// Tests the connection to the data source
    /// </summary>
    /// <param name="connectionParams">Connection parameters as a dictionary</param>
    /// <returns>True if the connection is successful, otherwise false</returns>
    Task<bool> TestConnectionAsync(IDictionary<string, string> connectionParams);
    
    /// <summary>
    /// Extracts data from the source
    /// </summary>
    /// <param name="connectionParams">Connection parameters as a dictionary</param>
    /// <param name="extractionParams">Additional parameters for extraction process</param>
    /// <returns>Extracted data in a standardized format</returns>
    Task<IEnumerable<IDictionary<string, object>>> ExtractDataAsync(
        IDictionary<string, string> connectionParams, 
        IDictionary<string, object>? extractionParams = null);
} 