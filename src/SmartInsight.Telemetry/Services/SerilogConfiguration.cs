using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SmartInsight.Telemetry.Options;

namespace SmartInsight.Telemetry.Services
{
    /// <summary>
    /// Service for configuring and initializing Serilog
    /// </summary>
    public static class SerilogConfiguration
    {
        /// <summary>
        /// Configures Serilog with the provided options
        /// </summary>
        /// <param name="options">The Serilog configuration options</param>
        /// <param name="environmentName">The name of the current environment (e.g., "Development", "Production")</param>
        /// <param name="configuration">The application configuration</param>
        /// <returns>The configured logger</returns>
        public static Logger CreateLogger(SerilogOptions options, string environmentName, IConfiguration configuration)
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(GetLogEventLevel(options.MinimumLevel));

            // Set override levels
            foreach (var override_ in options.OverrideMinimumLevel)
            {
                if (Enum.TryParse<LogEventLevel>(override_.Value, true, out var level))
                {
                    loggerConfig = loggerConfig.MinimumLevel.Override(override_.Key, level);
                }
            }

            // Add enrichers
            loggerConfig = loggerConfig.Enrich.FromLogContext();
            
            if (options.EnrichWithMachineName)
            {
                // Using WithEnvironmentUserName instead of WithMachineName due to ambiguous method reference
                loggerConfig = loggerConfig.Enrich.WithEnvironmentUserName();
            }
            
            if (options.EnrichWithEnvironment)
            {
                loggerConfig = loggerConfig.Enrich.WithEnvironmentName();
            }
            
            if (options.EnrichWithThreadId)
            {
                loggerConfig = loggerConfig.Enrich.WithThreadId();
            }

            // Add sinks
            bool consoleAdded = false;
            
            // Console sink (if enabled or in Development)
            if (options.WriteToConsole && (options.UseConsoleInDevelopment || !IsDevelopment(environmentName)))
            {
                loggerConfig = loggerConfig.WriteTo.Console();
                consoleAdded = true;
            }
            
            // File sink (if enabled)
            if (options.WriteToFile)
            {
                string filePath = options.FilePath;
                
                // Create directory if it doesn't exist
                string? directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                
                // Convert rolling interval to Serilog's RollingInterval
                var rollingInterval = GetRollingInterval(options.RollingInterval);
                
                // Calculate file size limit (10 MB default if null)
                long? fileSizeLimit = options.FileSizeLimitMB.HasValue 
                    ? options.FileSizeLimitMB.Value * 1024 * 1024
                    : 10 * 1024 * 1024;
                
                loggerConfig = loggerConfig.WriteTo.File(
                    filePath,
                    rollingInterval: rollingInterval,
                    fileSizeLimitBytes: fileSizeLimit,
                    retainedFileCountLimit: options.RetainedFileCount);
            }
            
            // SEQ Sink (if enabled)
            if (options.WriteToSeq && !string.IsNullOrEmpty(options.SeqServerUrl))
            {
                if (!string.IsNullOrEmpty(options.SeqApiKey))
                {
                    loggerConfig = loggerConfig.WriteTo.Seq(options.SeqServerUrl, apiKey: options.SeqApiKey);
                }
                else
                {
                    loggerConfig = loggerConfig.WriteTo.Seq(options.SeqServerUrl);
                }
            }
            
            // Add console in development as fallback
            if (IsDevelopment(environmentName) && !consoleAdded)
            {
                loggerConfig = loggerConfig.WriteTo.Console();
            }
            
            return loggerConfig.CreateLogger();
        }
        
        /// <summary>
        /// Determines if the environment is Development
        /// </summary>
        /// <param name="environmentName">The environment name</param>
        /// <returns>True if the environment is Development</returns>
        private static bool IsDevelopment(string environmentName)
        {
            return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Parses a string log level to a Serilog LogEventLevel
        /// </summary>
        /// <param name="level">String representation of the log level</param>
        /// <returns>The corresponding LogEventLevel</returns>
        private static LogEventLevel GetLogEventLevel(string level)
        {
            return Enum.TryParse<LogEventLevel>(level, true, out var logLevel)
                ? logLevel
                : LogEventLevel.Information;
        }
        
        /// <summary>
        /// Converts our rolling interval value to Serilog's RollingInterval enum
        /// </summary>
        /// <param name="interval">Rolling interval type</param>
        /// <returns>Serilog RollingInterval</returns>
        private static Serilog.RollingInterval GetRollingInterval(RollingIntervalType interval)
        {
            return interval switch
            {
                RollingIntervalType.Infinite => Serilog.RollingInterval.Infinite,
                RollingIntervalType.Day => Serilog.RollingInterval.Day,
                RollingIntervalType.Hour => Serilog.RollingInterval.Hour,
                RollingIntervalType.Minute => Serilog.RollingInterval.Minute,
                _ => Serilog.RollingInterval.Day // Default to daily
            };
        }

        /// <summary>
        /// Initializes Serilog from DI service provider
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="environmentName">The environment name</param>
        /// <param name="configuration">The configuration instance</param>
        public static void InitializeSerilog(
            IServiceProvider services,
            string environmentName,
            IConfiguration configuration)
        {
            var optionsService = services.GetService<IOptions<SerilogOptions>>();
            var options = optionsService?.Value ?? new SerilogOptions();
            
            if (!options.UseSerilog)
            {
                return;
            }

            var logger = CreateLogger(options, environmentName, configuration);
            
            Log.Logger = logger;
            
            // Set the preserveStaticLogger parameter based on options
            // This helps when hosting the application in-process with IIS
            if (options.PreserveStaticLogger)
            {
                // No need to use Serilog.Core.Serilog.Configure() - just set Log.Logger
                Log.CloseAndFlush();
                Log.Logger = logger;
            }
        }
    }
} 