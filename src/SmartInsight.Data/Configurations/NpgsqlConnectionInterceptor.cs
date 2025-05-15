using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.Data.Contexts;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Intercepts database connection events to set tenant ID for Row-Level Security
/// </summary>
public class NpgsqlConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new interceptor with the associated DB context
    /// </summary>
    /// <param name="dbContext">The application DB context</param>
    public NpgsqlConnectionInterceptor(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Called when a connection is opened
    /// </summary>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        // Set the tenant ID on the connection for RLS
        _dbContext.SetCurrentTenantForConnection(connection);
        base.ConnectionOpened(connection, eventData);
    }

    /// <summary>
    /// Called when a connection is opened asynchronously
    /// </summary>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        // Set the tenant ID on the connection for RLS
        _dbContext.SetCurrentTenantForConnection(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }
} 