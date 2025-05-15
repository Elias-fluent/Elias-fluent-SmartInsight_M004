using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SmartInsight.Telemetry.Options;

namespace SmartInsight.Telemetry.Services
{
    /// <summary>
    /// Extension methods for adding Serilog to the service collection
    /// </summary>
    public static class SerilogServiceExtensions
    {
        /// <summary>
        /// Adds Serilog to the service collection with configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="environmentName">The environment name</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSerilogServices(
            this IServiceCollection services,
            IConfiguration configuration,
            string environmentName = "Production")
        {
            // Register SerilogOptions 
            services.Configure<SerilogOptions>(configuration.GetSection("Serilog"));
            
            // Configure Serilog
            var options = new SerilogOptions();
            configuration.GetSection("Serilog").Bind(options);
            
            if (options.UseSerilog)
            {
                var logger = SerilogConfiguration.CreateLogger(options, environmentName, configuration);
                
                // Set the static logger factory
                Log.Logger = logger;
                
                // Add Serilog logging provider
                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog(dispose: true);
                });
            }
            
            return services;
        }
        
        /// <summary>
        /// Configures the application to use Serilog
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="env">The hosting environment</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseSerilogConfiguration(
            this IApplicationBuilder app,
            IConfiguration configuration,
            IHostEnvironment env)
        {
            var options = new SerilogOptions();
            configuration.GetSection("Serilog").Bind(options);
            
            if (options.UseSerilog)
            {
                // Already configured in the extension method above
                // or in the Host.UseSerilogConfig() method
                
                // Use Serilog request logging middleware
                app.UseSerilogRequestLogging();
            }
            
            return app;
        }
    }
} 