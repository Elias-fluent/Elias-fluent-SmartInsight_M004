using System;
using Microsoft.Extensions.DependencyInjection;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Extension methods for connector registry services
/// </summary>
public static class ConnectorRegistryExtensions
{
    /// <summary>
    /// Adds connector registry services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddConnectorRegistry(this IServiceCollection services)
    {
        // Register the registry as a singleton
        services.AddSingleton<ConnectorRegistry>();
        
        return services;
    }
    
    /// <summary>
    /// Registers connector types with the registry
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectorTypes">Connector types to register</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection RegisterConnectorTypes(this IServiceCollection services, params Type[] connectorTypes)
    {
        // Registers a startup action to add connector types to the registry
        services.AddSingleton<ConnectorLifecycleExtensions.IStartupAction>(sp => new RegisterConnectorTypesStartupAction(
            sp.GetRequiredService<ConnectorRegistry>(),
            connectorTypes));
        
        return services;
    }
    
    /// <summary>
    /// Startup action to register connector types with the registry
    /// </summary>
    private class RegisterConnectorTypesStartupAction : ConnectorLifecycleExtensions.IStartupAction
    {
        private readonly ConnectorRegistry _registry;
        private readonly Type[] _connectorTypes;
        
        /// <summary>
        /// Creates a new register connector types startup action
        /// </summary>
        /// <param name="registry">Connector registry</param>
        /// <param name="connectorTypes">Connector types to register</param>
        public RegisterConnectorTypesStartupAction(
            ConnectorRegistry registry,
            Type[] connectorTypes)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _connectorTypes = connectorTypes ?? throw new ArgumentNullException(nameof(connectorTypes));
        }
        
        /// <summary>
        /// Executes the startup action
        /// </summary>
        public void Execute()
        {
            foreach (var connectorType in _connectorTypes)
            {
                _registry.RegisterConnectorTypeAsync(connectorType).Wait();
            }
        }
    }
} 