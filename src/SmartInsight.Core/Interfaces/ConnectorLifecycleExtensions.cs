using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Extension methods for connector lifecycle services
/// </summary>
public static class ConnectorLifecycleExtensions
{
    /// <summary>
    /// Adds connector event management services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddConnectorEventManagement(this IServiceCollection services)
    {
        // Register the event manager as a singleton
        services.AddSingleton<ConnectorEventManager>();
        
        return services;
    }
    
    /// <summary>
    /// Adds connector lifecycle logging to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddConnectorLifecycleLogging(this IServiceCollection services)
    {
        // Register the observer
        services.AddSingleton<IConnectorLifecycleObserver, ConnectorLoggingObserver>();
        
        // Register a startup action to link the observer to the event manager
        services.AddSingleton<IStartupAction>(sp => new ConnectorLoggingStartupAction(
            sp.GetRequiredService<ConnectorEventManager>(),
            sp.GetRequiredService<IConnectorLifecycleObserver>()));
        
        return services;
    }
    
    /// <summary>
    /// Adds all connector services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddConnectorServices(this IServiceCollection services)
    {
        // Add configuration
        services.AddConnectorConfiguration();
        
        // Add event management
        services.AddConnectorEventManagement();
        
        // Add registry
        services.AddConnectorRegistry();
        
        // Add lifecycle logging
        services.AddConnectorLifecycleLogging();
        
        return services;
    }
    
    /// <summary>
    /// Interface for service startup actions
    /// </summary>
    public interface IStartupAction
    {
        /// <summary>
        /// Executes the startup action
        /// </summary>
        void Execute();
    }
    
    /// <summary>
    /// Startup action to register the logging observer with the event manager
    /// </summary>
    private class ConnectorLoggingStartupAction : IStartupAction
    {
        private readonly ConnectorEventManager _eventManager;
        private readonly IConnectorLifecycleObserver _observer;
        
        /// <summary>
        /// Creates a new logging startup action
        /// </summary>
        /// <param name="eventManager">Event manager</param>
        /// <param name="observer">Observer to register</param>
        public ConnectorLoggingStartupAction(
            ConnectorEventManager eventManager,
            IConnectorLifecycleObserver observer)
        {
            _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }
        
        /// <summary>
        /// Executes the startup action
        /// </summary>
        public void Execute()
        {
            _eventManager.AddObserver(_observer);
        }
    }
} 