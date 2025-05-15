using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Knowledge.Connectors
{
    /// <summary>
    /// Documentation format types supported by the generator
    /// </summary>
    public enum DocumentationFormat
    {
        Markdown,
        Html,
        Json
    }

    /// <summary>
    /// Generates documentation for data source connectors based on their metadata
    /// </summary>
    public class ConnectorDocumentationGenerator
    {
        private readonly ILogger<ConnectorDocumentationGenerator> _logger;
        
        public ConnectorDocumentationGenerator(ILogger<ConnectorDocumentationGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Generates documentation for a specific connector
        /// </summary>
        /// <param name="connector">The connector instance</param>
        /// <param name="format">Output format (default: Markdown)</param>
        /// <returns>Documentation content as a string</returns>
        public string GenerateDocumentation(IDataSourceConnector connector, DocumentationFormat format = DocumentationFormat.Markdown)
        {
            try
            {
                var metadata = GetConnectorMetadata(connector);
                return GenerateFormattedDocumentation(metadata, format);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating documentation for connector {ConnectorId}", connector.Id);
                throw;
            }
        }
        
        /// <summary>
        /// Generates documentation for all registered connectors
        /// </summary>
        /// <param name="connectorRegistry">The connector registry</param>
        /// <param name="format">Output format (default: Markdown)</param>
        /// <returns>Dictionary of connector IDs mapped to their documentation</returns>
        public Dictionary<string, string> GenerateDocumentationForAll(
            IConnectorRegistry connectorRegistry, 
            DocumentationFormat format = DocumentationFormat.Markdown)
        {
            var result = new Dictionary<string, string>();
            
            try
            {
                foreach (var registration in connectorRegistry.GetRegisteredConnectors())
                {
                    try
                    {
                        // Get the connector type's metadata attribute
                        var connectorType = registration.ConnectorType;
                        var metadata = GetConnectorTypeMetadata(connectorType);
                        
                        if (metadata != null)
                        {
                            result[metadata.Id] = GenerateFormattedDocumentation(metadata, format);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error generating documentation for connector type {ConnectorType}", 
                            registration.ConnectorType.FullName);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating documentation for all connectors");
                throw;
            }
        }
        
        /// <summary>
        /// Writes documentation for all connectors to a directory
        /// </summary>
        /// <param name="connectorRegistry">The connector registry</param>
        /// <param name="outputDirectory">Directory to write documentation files</param>
        /// <param name="format">Output format (default: Markdown)</param>
        /// <returns>List of generated file paths</returns>
        public async Task<List<string>> WriteDocumentationToFilesAsync(
            IConnectorRegistry connectorRegistry,
            string outputDirectory,
            DocumentationFormat format = DocumentationFormat.Markdown)
        {
            var generatedFiles = new List<string>();
            var allDocs = GenerateDocumentationForAll(connectorRegistry, format);
            
            // Ensure the output directory exists
            Directory.CreateDirectory(outputDirectory);
            
            // Determine file extension based on format
            string extension = format switch
            {
                DocumentationFormat.Markdown => ".md",
                DocumentationFormat.Html => ".html",
                DocumentationFormat.Json => ".json",
                _ => ".txt"
            };
            
            // Write each connector's documentation to a file
            foreach (var (connectorId, content) in allDocs)
            {
                var filePath = Path.Combine(outputDirectory, $"{connectorId}{extension}");
                await File.WriteAllTextAsync(filePath, content);
                generatedFiles.Add(filePath);
            }
            
            // Generate an index file
            var indexContent = GenerateIndexFile(allDocs.Keys.ToList(), format);
            var indexFilePath = Path.Combine(outputDirectory, $"index{extension}");
            await File.WriteAllTextAsync(indexFilePath, indexContent);
            generatedFiles.Add(indexFilePath);
            
            return generatedFiles;
        }
        
        /// <summary>
        /// Extracts metadata from a connector instance
        /// </summary>
        private ConnectorMetadataAttribute GetConnectorMetadata(IDataSourceConnector connector)
        {
            // First try to get metadata from the instance itself
            var metadata = connector.GetMetadata();
            if (metadata != null && metadata.Count > 0)
            {
                // Convert the dictionary to a connector metadata attribute
                return new ConnectorMetadataAttribute(
                    connector.Id,
                    connector.Name,
                    connector.SourceType)
                {
                    Description = metadata.TryGetValue("description", out var desc) ? desc?.ToString() : connector.Description,
                    Version = metadata.TryGetValue("version", out var ver) ? ver?.ToString() : connector.Version,
                    Author = metadata.TryGetValue("author", out var author) ? author?.ToString() : null,
                    DocumentationUrl = metadata.TryGetValue("documentationUrl", out var docUrl) ? docUrl?.ToString() : null,
                    Capabilities = metadata.TryGetValue("capabilities", out var caps) ? 
                        ((caps as IEnumerable<string>)?.ToArray() ?? 
                         (caps as string)?.Split(',').Select(c => c.Trim()).ToArray()) : null,
                    ConnectionSchema = metadata.TryGetValue("connectionSchema", out var schema) ? schema?.ToString() : null
                };
            }
            
            // Fall back to the type's attribute
            return GetConnectorTypeMetadata(connector.GetType()) ??
                   new ConnectorMetadataAttribute(connector.Id, connector.Name, connector.SourceType)
                   {
                       Description = connector.Description,
                       Version = connector.Version
                   };
        }
        
        /// <summary>
        /// Extracts metadata attribute from a connector type
        /// </summary>
        private ConnectorMetadataAttribute? GetConnectorTypeMetadata(Type connectorType)
        {
            return connectorType.GetCustomAttribute<ConnectorMetadataAttribute>();
        }
        
        /// <summary>
        /// Generates formatted documentation based on the requested format
        /// </summary>
        private string GenerateFormattedDocumentation(ConnectorMetadataAttribute metadata, DocumentationFormat format)
        {
            return format switch
            {
                DocumentationFormat.Markdown => GenerateMarkdownDocumentation(metadata),
                DocumentationFormat.Html => GenerateHtmlDocumentation(metadata),
                DocumentationFormat.Json => GenerateJsonDocumentation(metadata),
                _ => GenerateMarkdownDocumentation(metadata) // Default to Markdown
            };
        }
        
        /// <summary>
        /// Generates Markdown documentation from connector metadata
        /// </summary>
        private string GenerateMarkdownDocumentation(ConnectorMetadataAttribute metadata)
        {
            var sb = new StringBuilder();
            
            // Add header
            sb.AppendLine($"# {metadata.Name} Connector");
            sb.AppendLine();
            
            // Add metadata table
            sb.AppendLine("## Connector Information");
            sb.AppendLine();
            sb.AppendLine("| Property | Value |");
            sb.AppendLine("| --- | --- |");
            sb.AppendLine($"| ID | `{metadata.Id}` |");
            sb.AppendLine($"| Source Type | {metadata.SourceType} |");
            sb.AppendLine($"| Version | {metadata.Version ?? "N/A"} |");
            
            if (!string.IsNullOrEmpty(metadata.Author))
            {
                sb.AppendLine($"| Author | {metadata.Author} |");
            }
            
            if (!string.IsNullOrEmpty(metadata.DocumentationUrl))
            {
                sb.AppendLine($"| Documentation | [{metadata.DocumentationUrl}]({metadata.DocumentationUrl}) |");
            }
            
            sb.AppendLine();
            
            // Add description
            if (!string.IsNullOrEmpty(metadata.Description))
            {
                sb.AppendLine("## Description");
                sb.AppendLine();
                sb.AppendLine(metadata.Description);
                sb.AppendLine();
            }
            
            // Add capabilities
            if (metadata.Capabilities != null && metadata.Capabilities.Length > 0)
            {
                sb.AppendLine("## Capabilities");
                sb.AppendLine();
                foreach (var capability in metadata.Capabilities)
                {
                    sb.AppendLine($"- {capability}");
                }
                sb.AppendLine();
            }
            
            // Add connection schema
            if (!string.IsNullOrEmpty(metadata.ConnectionSchema))
            {
                sb.AppendLine("## Connection Schema");
                sb.AppendLine();
                sb.AppendLine("```json");
                sb.AppendLine(metadata.ConnectionSchema);
                sb.AppendLine("```");
                sb.AppendLine();
            }
            
            // Add footer with generation timestamp
            sb.AppendLine("---");
            sb.AppendLine($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates HTML documentation from connector metadata
        /// </summary>
        private string GenerateHtmlDocumentation(ConnectorMetadataAttribute metadata)
        {
            var sb = new StringBuilder();
            
            // Basic HTML structure
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"  <title>{metadata.Name} Connector Documentation</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: system-ui, -apple-system, sans-serif; line-height: 1.5; max-width: 800px; margin: 0 auto; padding: 2rem; }");
            sb.AppendLine("    h1, h2 { color: #333; }");
            sb.AppendLine("    table { border-collapse: collapse; width: 100%; margin-bottom: 1rem; }");
            sb.AppendLine("    th, td { text-align: left; padding: 0.75rem; border-bottom: 1px solid #ddd; }");
            sb.AppendLine("    th { background-color: #f8f9fa; }");
            sb.AppendLine("    code { background-color: #f5f5f5; padding: 0.2rem 0.4rem; border-radius: 3px; font-family: monospace; }");
            sb.AppendLine("    pre { background-color: #f5f5f5; padding: 1rem; border-radius: 5px; overflow-x: auto; }");
            sb.AppendLine("    .footer { margin-top: 2rem; font-size: 0.8rem; color: #666; border-top: 1px solid #eee; padding-top: 1rem; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            // Header
            sb.AppendLine($"  <h1>{metadata.Name} Connector</h1>");
            
            // Metadata table
            sb.AppendLine("  <h2>Connector Information</h2>");
            sb.AppendLine("  <table>");
            sb.AppendLine("    <tr><th>Property</th><th>Value</th></tr>");
            sb.AppendLine($"    <tr><td>ID</td><td><code>{metadata.Id}</code></td></tr>");
            sb.AppendLine($"    <tr><td>Source Type</td><td>{metadata.SourceType}</td></tr>");
            sb.AppendLine($"    <tr><td>Version</td><td>{metadata.Version ?? "N/A"}</td></tr>");
            
            if (!string.IsNullOrEmpty(metadata.Author))
            {
                sb.AppendLine($"    <tr><td>Author</td><td>{metadata.Author}</td></tr>");
            }
            
            if (!string.IsNullOrEmpty(metadata.DocumentationUrl))
            {
                sb.AppendLine($"    <tr><td>Documentation</td><td><a href=\"{metadata.DocumentationUrl}\">{metadata.DocumentationUrl}</a></td></tr>");
            }
            
            sb.AppendLine("  </table>");
            
            // Description
            if (!string.IsNullOrEmpty(metadata.Description))
            {
                sb.AppendLine("  <h2>Description</h2>");
                sb.AppendLine($"  <p>{metadata.Description}</p>");
            }
            
            // Capabilities
            if (metadata.Capabilities != null && metadata.Capabilities.Length > 0)
            {
                sb.AppendLine("  <h2>Capabilities</h2>");
                sb.AppendLine("  <ul>");
                foreach (var capability in metadata.Capabilities)
                {
                    sb.AppendLine($"    <li>{capability}</li>");
                }
                sb.AppendLine("  </ul>");
            }
            
            // Connection schema
            if (!string.IsNullOrEmpty(metadata.ConnectionSchema))
            {
                sb.AppendLine("  <h2>Connection Schema</h2>");
                sb.AppendLine("  <pre>");
                sb.AppendLine(metadata.ConnectionSchema);
                sb.AppendLine("  </pre>");
            }
            
            // Footer
            sb.AppendLine("  <div class=\"footer\">");
            sb.AppendLine($"    Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine("  </div>");
            
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates JSON documentation from connector metadata
        /// </summary>
        private string GenerateJsonDocumentation(ConnectorMetadataAttribute metadata)
        {
            // Create anonymous object that will be serialized to JSON
            var doc = new
            {
                id = metadata.Id,
                name = metadata.Name,
                sourceType = metadata.SourceType,
                description = metadata.Description,
                version = metadata.Version,
                author = metadata.Author,
                documentationUrl = metadata.DocumentationUrl,
                capabilities = metadata.Capabilities,
                connectionSchema = metadata.ConnectionSchema,
                generatedOn = DateTime.UtcNow
            };
            
            // Serialize to JSON with indentation
            return System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        
        /// <summary>
        /// Generates an index file listing all available connectors
        /// </summary>
        private string GenerateIndexFile(List<string> connectorIds, DocumentationFormat format)
        {
            return format switch
            {
                DocumentationFormat.Markdown => GenerateMarkdownIndex(connectorIds),
                DocumentationFormat.Html => GenerateHtmlIndex(connectorIds),
                DocumentationFormat.Json => GenerateJsonIndex(connectorIds),
                _ => GenerateMarkdownIndex(connectorIds)
            };
        }
        
        /// <summary>
        /// Generates a Markdown index file
        /// </summary>
        private string GenerateMarkdownIndex(List<string> connectorIds)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("# Available Connectors");
            sb.AppendLine();
            sb.AppendLine("This document lists all available connectors in the system.");
            sb.AppendLine();
            
            foreach (var id in connectorIds.OrderBy(id => id))
            {
                sb.AppendLine($"- [{id}](./{id}.md)");
            }
            
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates an HTML index file
        /// </summary>
        private string GenerateHtmlIndex(List<string> connectorIds)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("  <title>Available Connectors</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: system-ui, -apple-system, sans-serif; line-height: 1.5; max-width: 800px; margin: 0 auto; padding: 2rem; }");
            sb.AppendLine("    h1 { color: #333; }");
            sb.AppendLine("    ul { padding-left: 1.5rem; }");
            sb.AppendLine("    .footer { margin-top: 2rem; font-size: 0.8rem; color: #666; border-top: 1px solid #eee; padding-top: 1rem; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            
            sb.AppendLine("  <h1>Available Connectors</h1>");
            sb.AppendLine("  <p>This document lists all available connectors in the system.</p>");
            
            sb.AppendLine("  <ul>");
            foreach (var id in connectorIds.OrderBy(id => id))
            {
                sb.AppendLine($"    <li><a href=\"./{id}.html\">{id}</a></li>");
            }
            sb.AppendLine("  </ul>");
            
            sb.AppendLine("  <div class=\"footer\">");
            sb.AppendLine($"    Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine("  </div>");
            
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates a JSON index file
        /// </summary>
        private string GenerateJsonIndex(List<string> connectorIds)
        {
            var doc = new
            {
                title = "Available Connectors",
                description = "This document lists all available connectors in the system.",
                connectors = connectorIds.OrderBy(id => id).Select(id => new
                {
                    id,
                    url = $"./{id}.json"
                }).ToList(),
                generatedOn = DateTime.UtcNow
            };
            
            return System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
} 