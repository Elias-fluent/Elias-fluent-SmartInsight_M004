namespace SmartInsight.Core.Exceptions;

/// <summary>
/// Exception for data source related errors
/// </summary>
public class DataSourceException : SmartInsightException
{
    /// <summary>
    /// ID of the data source that caused the exception
    /// </summary>
    public string? DataSourceId { get; }
    
    /// <summary>
    /// Type of the data source
    /// </summary>
    public string? DataSourceType { get; }
    
    /// <summary>
    /// Creates a new data source exception
    /// </summary>
    public DataSourceException() : base("An error occurred with the data source.", "DATASOURCE_ERROR")
    {
    }
    
    /// <summary>
    /// Creates a new data source exception with a message
    /// </summary>
    /// <param name="message">Exception message</param>
    public DataSourceException(string message) : base(message, "DATASOURCE_ERROR")
    {
    }
    
    /// <summary>
    /// Creates a new data source exception with message and data source information
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="dataSourceId">ID of the data source</param>
    /// <param name="dataSourceType">Type of the data source</param>
    public DataSourceException(string message, string dataSourceId, string dataSourceType) 
        : base(message, "DATASOURCE_ERROR")
    {
        DataSourceId = dataSourceId;
        DataSourceType = dataSourceType;
    }
    
    /// <summary>
    /// Creates a new data source exception with a message and inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception</param>
    public DataSourceException(string message, Exception innerException) 
        : base(message, "DATASOURCE_ERROR", innerException)
    {
    }
    
    /// <summary>
    /// Creates a new data source exception with message, data source information, and inner exception
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="dataSourceId">ID of the data source</param>
    /// <param name="dataSourceType">Type of the data source</param>
    /// <param name="innerException">Inner exception</param>
    public DataSourceException(string message, string dataSourceId, string dataSourceType, Exception innerException) 
        : base(message, "DATASOURCE_ERROR", innerException)
    {
        DataSourceId = dataSourceId;
        DataSourceType = dataSourceType;
    }
} 