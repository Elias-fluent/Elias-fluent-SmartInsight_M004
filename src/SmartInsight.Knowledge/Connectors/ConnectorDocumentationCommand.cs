using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Command-line utility for generating connector documentation
    /// </summary>
    public static class ConnectorDocumentationCommand
    {
        /// <summary>
        /// Builds a command-line command for generating connector documentation
        /// </summary>
        /// <returns>A configured command</returns>
        public static Command BuildCommand()
        {
            var outputOption = new Option<string>(
                "--output",
                () => Path.Combine(Environment.CurrentDirectory, "docs", "connectors"),
                "Output directory for documentation");
            outputOption.AddAlias("-o");
                
            var formatOption = new Option<DocumentationFormat>(
                "--format",
                () => DocumentationFormat.Markdown,
                "Documentation format");
            formatOption.AddAlias("-f");
                
            var verboseOption = new Option<bool>(
                "--verbose",
                () => false,
                "Enable verbose output");
            verboseOption.AddAlias("-v");
            
            var command = new Command("generate-docs", "Generate documentation for connectors");
            command.AddOption(outputOption);
            command.AddOption(formatOption);
            command.AddOption(verboseOption);
            
            command.SetHandler(
                (string output, DocumentationFormat format, bool verbose) => ExecuteAsync(output, format, verbose),
                outputOption, formatOption, verboseOption);
            
            return command;
        }
        
        /// <summary>
        /// Executes the documentation generation command
        /// </summary>
        private static async Task<int> ExecuteAsync(string output, DocumentationFormat format, bool verbose)
        {
            try
            {
                // Setup dependency injection
                var services = new ServiceCollection();
                
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
                });
                
                // Register the connector registry and documentation generator
                services.AddSingleton<IConnectorRegistry, ConnectorRegistry>();
                services.AddSingleton<ConnectorDocumentationGenerator>();
                
                // Build the service provider
                var serviceProvider = services.BuildServiceProvider();
                
                // Get required services
                var logger = serviceProvider.GetRequiredService<ILogger<ConnectorRegistry>>();
                var registry = serviceProvider.GetRequiredService<IConnectorRegistry>();
                var generator = serviceProvider.GetRequiredService<ConnectorDocumentationGenerator>();
                
                // Register all available connectors
                logger.LogInformation("Scanning for connectors...");
                int count = ((ConnectorRegistry)registry).RegisterAllConnectors();
                
                if (count == 0)
                {
                    logger.LogWarning("No connectors found. Documentation will be empty.");
                }
                else
                {
                    logger.LogInformation("Found {ConnectorCount} connectors", count);
                }
                
                // Generate documentation
                logger.LogInformation("Generating {Format} documentation in {OutputDirectory}...", 
                    format, Path.GetFullPath(output));
                
                var files = await generator.WriteDocumentationToFilesAsync(registry, output, format);
                
                logger.LogInformation("Documentation generated successfully. Generated {FileCount} files:", files.Count);
                foreach (var file in files)
                {
                    logger.LogInformation("  - {FilePath}", Path.GetRelativePath(Environment.CurrentDirectory, file));
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                if (verbose)
                {
                    Console.Error.WriteLine(ex.ToString());
                }
                return 1;
            }
        }
    }
} 