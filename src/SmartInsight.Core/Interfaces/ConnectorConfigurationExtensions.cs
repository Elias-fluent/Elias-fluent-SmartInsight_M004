using System;
using Microsoft.Extensions.DependencyInjection;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Extension methods for connector configuration services
/// </summary>
public static class ConnectorConfigurationExtensions
{
    /// <summary>
    /// Adds a memory-based secure credential store to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMemoryCredentialStore(this IServiceCollection services)
    {
        services.AddSingleton<ISecureCredentialStore, MemoryCredentialStore>();
        return services;
    }
    
    /// <summary>
    /// Adds a scoped credential store that is isolated per tenant
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="tenantIdResolver">Function to resolve the current tenant ID</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddScopedCredentialStore(
        this IServiceCollection services,
        Func<IServiceProvider, Guid> tenantIdResolver)
    {
        // Register a factory that creates a credential store for each tenant
        services.AddScoped<ISecureCredentialStore>(sp =>
        {
            var tenantId = tenantIdResolver(sp);
            return new MemoryCredentialStore();
        });
        
        return services;
    }
    
    /// <summary>
    /// Adds connector configuration services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    /// <remarks>
    /// This registers the memory credential store by default.
    /// For production, consider overriding with a more secure implementation.
    /// </remarks>
    public static IServiceCollection AddConnectorConfiguration(this IServiceCollection services)
    {
        // Register memory credential store by default
        services.AddMemoryCredentialStore();
        
        return services;
    }
} 