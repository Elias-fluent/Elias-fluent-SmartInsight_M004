using SmartInsight.Core.DTOs;
using SmartInsight.Core.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartInsight.Knowledge.Connectors.Interfaces
{
    /// <summary>
    /// Interface for testing connections to data sources
    /// </summary>
    public interface IConnectionTester
    {
        /// <summary>
        /// Tests a connection to a data source
        /// </summary>
        /// <param name="sourceType">Type of data source</param>
        /// <param name="connectionParameters">Connection parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Connection test result</returns>
        Task<ConnectionTestResultDto> TestConnectionAsync(
            DataSourceType sourceType, 
            IDictionary<string, string> connectionParameters, 
            CancellationToken cancellationToken = default);
    }
} 