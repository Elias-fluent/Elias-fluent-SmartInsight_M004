using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Extension methods for registering connector services
    /// </summary>
    public static class ConnectorServiceExtensions
    {
        /// <summary>
        /// Adds connector services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddConnectorServices(this IServiceCollection services)
        {
            // Register the connector registry
            services.TryAddSingleton<IConnectorRegistry, ConnectorRegistry>();
            
            // Register the connector factory
            services.TryAddSingleton<IConnectorFactory, ConnectorFactory>();
            
            // Register the connector provider
            services.TryAddSingleton<ConnectorProvider>();
            
            return services;
        }
        
        /// <summary>
        /// Adds a specific connector type to the service collection
        /// </summary>
        /// <typeparam name="T">The connector type to add</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="lifetime">The service lifetime (default: Transient)</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddConnector<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient) 
            where T : class, IDataSourceConnector
        {
            // Register the concrete connector type with the specified lifetime
            services.Add(new ServiceDescriptor(typeof(T), typeof(T), lifetime));
            
            // Register it as IDataSourceConnector as well
            services.Add(new ServiceDescriptor(typeof(IDataSourceConnector), 
                serviceProvider => serviceProvider.GetRequiredService<T>(), 
                lifetime));
            
            return services;
        }
        
        /// <summary>
        /// Adds all connector types from the specified assembly to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="assembly">The assembly to scan for connector types</param>
        /// <param name="lifetime">The service lifetime (default: Transient)</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddConnectorsFromAssembly(
            this IServiceCollection services,
            Assembly assembly,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
                
            // Find all non-abstract types that implement IDataSourceConnector
            var connectorTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => typeof(IDataSourceConnector).IsAssignableFrom(t))
                .Where(t => t.GetCustomAttribute<ConnectorMetadataAttribute>() != null);
                
            // Register each connector type
            foreach (var type in connectorTypes)
            {
                // Register the concrete type
                services.Add(new ServiceDescriptor(type, type, lifetime));
                
                // Also register as IDataSourceConnector
                services.Add(new ServiceDescriptor(
                    typeof(IDataSourceConnector),
                    serviceProvider => serviceProvider.GetRequiredService(type),
                    lifetime));
            }
            
            return services;
        }
    }
} 