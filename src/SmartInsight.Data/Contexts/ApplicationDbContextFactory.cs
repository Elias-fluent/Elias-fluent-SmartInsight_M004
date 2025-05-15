using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using SmartInsight.Core.Interfaces;
using SmartInsight.Data.Configurations;

namespace SmartInsight.Data.Contexts;

/// <summary>
/// Factory for creating ApplicationDbContext instances at design-time
/// Used by EF Core tools (like migrations) when a context is needed
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Creates a new ApplicationDbContext for design-time tools
    /// </summary>
    /// <param name="args">Command-line arguments</param>
    /// <returns>A configured ApplicationDbContext</returns>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Get the connection string
        // In a real scenario, this would be loaded from appsettings.json
        // but for simplicity and design-time tools, we'll use a hardcoded string
        var connectionString = "Host=localhost;Database=smartinsight;Username=postgres;Password=postgres";
        
        // Create options for the context
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, options => {
                options.MigrationsAssembly("SmartInsight.Data");
                options.EnableRetryOnFailure(5);
                options.CommandTimeout(30);
            });
            
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Runtime factory for creating ApplicationDbContext instances with tenant context
/// </summary>
public class ApplicationDbContextFactoryRuntime
{
    private readonly IConfiguration _configuration;
    private readonly ITenantAccessor? _tenantAccessor;
    private readonly IConnectionStringManager _connectionStringManager;
    
    /// <summary>
    /// Initializes a new factory with necessary dependencies
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="tenantAccessor">Optional tenant accessor for multi-tenant isolation</param>
    /// <param name="connectionStringManager">Optional connection string manager</param>
    public ApplicationDbContextFactoryRuntime(
        IConfiguration configuration,
        ITenantAccessor? tenantAccessor = null,
        IConnectionStringManager? connectionStringManager = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tenantAccessor = tenantAccessor;
        _connectionStringManager = connectionStringManager ?? new ConnectionStringManager(configuration, tenantAccessor);
    }
    
    /// <summary>
    /// Creates a new ApplicationDbContext with the correct connection string
    /// </summary>
    /// <param name="useSpecificTenant">Whether to use a specific tenant's connection</param>
    /// <returns>A configured ApplicationDbContext</returns>
    public ApplicationDbContext CreateDbContext(bool useSpecificTenant = true)
    {
        // Get a connection string with tenant-specific application name
        var connectionString = _connectionStringManager.GetConnectionStringWithApplicationName(
            "DefaultConnection", 
            "SmartInsight");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string not found.");
        }
        
        // Create options for the context
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, options => {
                options.MigrationsAssembly("SmartInsight.Data");
                options.EnableRetryOnFailure(5);
                options.CommandTimeout(30);
            });
            
        return new ApplicationDbContext(optionsBuilder.Options, _tenantAccessor);
    }
} 