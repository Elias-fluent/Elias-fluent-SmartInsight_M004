using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Knowledge.Connectors.Examples
{
    /// <summary>
    /// Example demonstrating the usage of connector factory and sample connector
    /// </summary>
    public class ConnectorFactoryExample
    {
        /// <summary>
        /// Runs the connector factory example
        /// </summary>
        public static async Task RunExampleAsync()
        {
            Console.WriteLine("Starting Connector Factory Example...");
            
            // Set up dependency injection
            var serviceProvider = ConfigureServices();
            
            try
            {
                // Get the connector factory from DI
                var factory = serviceProvider.GetRequiredService<IConnectorFactory>();
                
                // Get all available connector IDs
                Console.WriteLine("Available connectors:");
                var connectorIds = factory.GetAvailableConnectorIds();
                foreach (var id in connectorIds)
                {
                    Console.WriteLine($"  - {id}");
                }
                
                // Create a sample connector instance
                Console.WriteLine("\nCreating sample connector instance...");
                var connector = factory.CreateConnector("sample-connector");
                
                // Display connector metadata
                var metadata = connector.GetMetadata();
                Console.WriteLine("Sample Connector Metadata:");
                foreach (var item in metadata)
                {
                    Console.WriteLine($"  {item.Key}: {item.Value}");
                }
                
                // Display connector capabilities
                var capabilities = connector.GetCapabilities();
                Console.WriteLine("\nSample Connector Capabilities:");
                Console.WriteLine($"  Supports Incremental: {capabilities.SupportsIncremental}");
                Console.WriteLine($"  Supports Schema Discovery: {capabilities.SupportsSchemaDiscovery}");
                Console.WriteLine($"  Supports Advanced Filtering: {capabilities.SupportsAdvancedFiltering}");
                Console.WriteLine($"  Supports Preview: {capabilities.SupportsPreview}");
                Console.WriteLine($"  Max Concurrent Extractions: {capabilities.MaxConcurrentExtractions}");
                Console.WriteLine($"  Supported Authentication Types: {string.Join(", ", capabilities.SupportedAuthentications)}");
                Console.WriteLine($"  Supported Source Types: {string.Join(", ", capabilities.SupportedSourceTypes)}");
                
                // Create configuration for the connector
                Console.WriteLine("\nCreating connector configuration...");
                var credentials = new InMemoryCredentialStore();
                credentials.StoreCredential("apiKey", "sample-api-key-12345");
                
                var config = new ConnectorConfiguration(
                    connectorId: "sample-connector",
                    name: "Sample Config",
                    tenantId: Guid.NewGuid(),
                    credentials: credentials,
                    connectionParameters: new Dictionary<string, string>
                    {
                        ["server"] = "sample-server.example.com",
                        ["port"] = "1234",
                        ["useTls"] = "true"
                    },
                    settings: new Dictionary<string, object>
                    {
                        ["maxRecords"] = 500,
                        ["timeout"] = 30
                    });
                
                // Initialize the connector
                Console.WriteLine("Initializing connector...");
                bool initialized = await connector.InitializeAsync(config);
                Console.WriteLine($"Connector initialized: {initialized}");
                
                // Connect to the sample data source
                Console.WriteLine("\nConnecting to sample data source...");
                var connectionParams = new Dictionary<string, string>
                {
                    ["server"] = "sample-server.example.com",
                    ["port"] = "1234",
                    ["apiKey"] = "sample-api-key-12345",
                    ["useTls"] = "true"
                };
                
                var connectionResult = await connector.ConnectAsync(connectionParams);
                Console.WriteLine($"Connection result: {(connectionResult.IsSuccess ? "Success" : "Failure")}");
                
                if (connectionResult.IsSuccess)
                {
                    Console.WriteLine($"Connection ID: {connectionResult.ConnectionId}");
                    Console.WriteLine($"Server Version: {connectionResult.ServerVersion}");
                    Console.WriteLine("Connection Info:");
                    foreach (var info in connectionResult.ConnectionInfo)
                    {
                        Console.WriteLine($"  {info.Key}: {info.Value}");
                    }
                    
                    // Discover data structures
                    Console.WriteLine("\nDiscovering data structures...");
                    var structures = await connector.DiscoverDataStructuresAsync();
                    Console.WriteLine("Available data structures:");
                    foreach (var structure in structures)
                    {
                        Console.WriteLine($"  - {structure.Name} ({structure.Type}): {structure.Description}");
                        Console.WriteLine("    Fields:");
                        foreach (var field in structure.Fields)
                        {
                            Console.WriteLine($"      {field.Name} ({field.DataType}){(field.IsRequired ? " [Required]" : "")}");
                        }
                    }
                    
                    // Extract data from the first structure
                    if (structures.GetEnumerator().MoveNext())
                    {
                        var firstStructure = structures.GetEnumerator().Current;
                        Console.WriteLine($"\nExtracting data from {firstStructure.Name}...");
                        
                        // Register for progress events
                        connector.ProgressChanged += (sender, args) =>
                        {
                            Console.WriteLine($"Progress: {args.ProgressPercentage}% - {args.Message}");
                        };
                        
                        var extractionParams = new ExtractionParameters(
                            targetStructures: new[] { firstStructure.Name },
                            maxRecords: 5);
                            
                        var extractionResult = await connector.ExtractDataAsync(extractionParams);
                        
                        if (extractionResult.IsSuccess)
                        {
                            Console.WriteLine($"Extracted {extractionResult.Data.Count} records");
                            
                            // Display the first record
                            if (extractionResult.Data.Count > 0)
                            {
                                Console.WriteLine("First record:");
                                foreach (var field in extractionResult.Data[0])
                                {
                                    Console.WriteLine($"  {field.Key}: {field.Value}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Extraction failed: {extractionResult.ErrorMessage}");
                        }
                    }
                    
                    // Disconnect
                    Console.WriteLine("\nDisconnecting...");
                    bool disconnected = await connector.DisconnectAsync();
                    Console.WriteLine($"Disconnected: {disconnected}");
                }
                else
                {
                    Console.WriteLine($"Connection failed: {connectionResult.ErrorMessage}");
                    if (connectionResult.Errors.Count > 0)
                    {
                        Console.WriteLine("Errors:");
                        foreach (var error in connectionResult.Errors)
                        {
                            Console.WriteLine($"  {error.FieldName}: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("\nConnector Factory Example completed.");
        }
        
        /// <summary>
        /// Configures the dependency injection container
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            // Add connector services
            services.AddConnectorServices();
            
            // Register the sample connector directly
            services.AddConnector<SampleConnector>();
            
            return services.BuildServiceProvider();
        }
        
        /// <summary>
        /// Simple in-memory credential store implementation for the example
        /// </summary>
        private class InMemoryCredentialStore : ISecureCredentialStore
        {
            private readonly Dictionary<string, string> _credentials = new Dictionary<string, string>();
            
            public void StoreCredential(string key, string value)
            {
                _credentials[key] = value;
            }
            
            public void SetCredential(string key, string value)
            {
                _credentials[key] = value;
            }
            
            public string? GetCredential(string key)
            {
                return _credentials.TryGetValue(key, out var value) ? value : null;
            }
            
            public bool HasCredential(string key)
            {
                return _credentials.ContainsKey(key);
            }
            
            public bool RemoveCredential(string key)
            {
                return _credentials.Remove(key);
            }
            
            public void Clear()
            {
                _credentials.Clear();
            }
            
            public IEnumerable<string> GetCredentialKeys()
            {
                return _credentials.Keys;
            }
        }
    }
} 