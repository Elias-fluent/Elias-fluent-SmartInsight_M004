using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Core.Interfaces;
using SmartInsight.Data.Contexts;
using System;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Extension methods for setting up data-related services
/// </summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Adds database-related services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register connection string manager
        services.AddSingleton<IConnectionStringManager, ConnectionStringManager>();
        
        // Register DbContext factory
        services.AddSingleton<ApplicationDbContextFactoryRuntime>();
        
        // Register DbContext
        services.AddDbContext<ApplicationDbContext>((provider, options) => 
        {
            var factory = provider.GetRequiredService<ApplicationDbContextFactoryRuntime>();
            var dbContext = factory.CreateDbContext();
            
            // Copy settings from the factory-created context to our options
            options.UseNpgsql(
                dbContext.Database.GetDbConnection().ConnectionString,
                npgsql => 
                {
                    npgsql.MigrationsAssembly("SmartInsight.Data");
                    npgsql.EnableRetryOnFailure(5);
                    npgsql.CommandTimeout(30);
                });
        });
        
        return services;
    }
    
    /// <summary>
    /// Adds a scoped DbContext that uses the tenant-specific connection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddTenantDbContext(this IServiceCollection services)
    {
        services.AddScoped(provider => 
        {
            var factory = provider.GetRequiredService<ApplicationDbContextFactoryRuntime>();
            return factory.CreateDbContext(useSpecificTenant: true);
        });
        
        return services;
    }
} 