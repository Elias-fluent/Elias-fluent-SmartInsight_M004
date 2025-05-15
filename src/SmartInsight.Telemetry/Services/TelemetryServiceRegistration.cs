using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SmartInsight.Telemetry.Interfaces;
using SmartInsight.Telemetry.Options;
using SmartInsight.Telemetry.Repositories;

namespace SmartInsight.Telemetry.Services
{
    /// <summary>
    /// Extension methods for registering telemetry services
    /// </summary>
    public static class TelemetryServiceRegistration
    {
        /// <summary>
        /// Adds all telemetry services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="environmentName">The environment name</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddTelemetryServices(
            this IServiceCollection services,
            IConfiguration configuration,
            string environmentName = "Production")
        {
            // Register options
            services.Configure<TelemetryOptions>(configuration.GetSection("Telemetry"));
            services.Configure<SerilogOptions>(configuration.GetSection("Serilog"));
            
            // Register telemetry repository
            services.AddSingleton<ITelemetryRepository, InMemoryTelemetryRepository>();
            
            // Register telemetry service
            services.AddSingleton<ITelemetryService, TelemetryService>();
            
            // Configure Serilog
            services.AddSerilogServices(configuration, environmentName);
            
            return services;
        }
    }
} 