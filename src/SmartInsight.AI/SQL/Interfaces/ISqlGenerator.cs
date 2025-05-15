using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for generating safe SQL from templates and parameters
    /// </summary>
    public interface ISqlGenerator
    {
        /// <summary>
        /// Generates SQL from a template and parameters
        /// </summary>
        /// <param name="template">The SQL template</param>
        /// <param name="parameters">The parameter values</param>
        /// <param name="tenantContext">Optional tenant context for isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The SQL generation result</returns>
        Task<SqlGenerationResult> GenerateSqlAsync(
            SqlTemplate template, 
            Dictionary<string, object> parameters, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates parameterized SQL with proper parameter handling
        /// </summary>
        /// <param name="template">The SQL template</param>
        /// <param name="parameters">The parameter values</param>
        /// <param name="tenantContext">Optional tenant context for isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The SQL generation result with parameters for parameterized execution</returns>
        Task<SqlGenerationResult> GenerateParameterizedSqlAsync(
            SqlTemplate template, 
            Dictionary<string, object> parameters, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates SQL directly from a natural language query
        /// </summary>
        /// <param name="query">The natural language query</param>
        /// <param name="tenantContext">Optional tenant context for isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The SQL generation result</returns>
        Task<SqlGenerationResult> GenerateSqlFromQueryAsync(
            string query, 
            TenantContext? tenantContext = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines the type of SQL operation (SELECT, INSERT, UPDATE, DELETE)
        /// </summary>
        /// <param name="sql">The SQL query</param>
        /// <returns>The SQL operation type</returns>
        SqlOperationType DetermineSqlOperationType(string sql);
    }
} 