using SmartInsight.Core.Interfaces;
using System;

namespace SmartInsight.Knowledge.Connectors.Interfaces;

/// <summary>
/// Factory interface for creating data source connectors
/// </summary>
public interface IDataSourceConnectorFactory
{
    /// <summary>
    /// Gets a connector for the specified data source type
    /// </summary>
    /// <param name="dataSourceType">The type of data source</param>
    /// <returns>A data source connector or null if not found</returns>
    IDataSourceConnector? GetConnector(string dataSourceType);
} 