using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using SmartInsight.Telemetry.Options;

namespace SmartInsight.Telemetry.Services
{
    /// <summary>
    /// Extension methods for configuring Serilog with ASP.NET Core HostBuilder
    /// </summary>
    public static class SerilogWebHostExtensions
    {
        /// <summary>
        /// Configures Serilog for an IHostBuilder
        /// </summary>
        /// <param name="builder">The host builder</param>
        /// <param name="configureOptions">Optional action to configure Serilog options</param>
        /// <returns>The host builder</returns>
        public static IHostBuilder UseSerilogConfig(
            this IHostBuilder builder, 
            Action<SerilogOptions>? configureOptions = null)
        {
            return builder.UseSerilog((hostingContext, services, loggerConfiguration) =>
            {
                var options = new SerilogOptions();
                
                // Bind from configuration
                hostingContext.Configuration.GetSection("Serilog").Bind(options);
                
                // Apply custom configuration if provided
                configureOptions?.Invoke(options);
                
                if (!options.UseSerilog)
                {
                    return;
                }
                
                // Create logger with final configuration
                var logger = SerilogConfiguration.CreateLogger(
                    options,
                    hostingContext.HostingEnvironment.EnvironmentName,
                    hostingContext.Configuration);
                
                // Set the base logger
                Log.Logger = logger;
            });
        }
    }
} 