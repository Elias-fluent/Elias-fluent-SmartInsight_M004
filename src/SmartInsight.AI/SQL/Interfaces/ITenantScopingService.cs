using System;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for enforcing tenant isolation in SQL queries
    /// </summary>
    public interface ITenantScopingService
    {
        /// <summary>
        /// Applies tenant isolation to a SQL query
        /// </summary>
        /// <param name="sql">The original SQL query</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>SQL query with tenant isolation applied</returns>
        Task<string> ApplyTenantScopingAsync(
            string sql, 
            TenantContext tenantContext, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current tenant context
        /// </summary>
        /// <param name="tenantId">Optional tenant ID. If null, uses the current tenant</param>
        /// <param name="userId">Optional user ID. If null, uses the current user</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The tenant context</returns>
        Task<TenantContext> GetTenantContextAsync(
            Guid? tenantId = null, 
            Guid? userId = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a user has permission to access a tenant
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="tenantId">The tenant ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the user has access, false otherwise</returns>
        Task<bool> ValidateTenantAccessAsync(
            Guid userId, 
            Guid tenantId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that a SQL query properly enforces tenant isolation
        /// </summary>
        /// <param name="sql">The SQL query to validate</param>
        /// <param name="tenantContext">The tenant context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the query enforces isolation, false otherwise</returns>
        Task<bool> ValidateTenantIsolationAsync(
            string sql, 
            TenantContext tenantContext, 
            CancellationToken cancellationToken = default);
    }
} 