using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction
{
    /// <summary>
    /// Base implementation of an entity extractor that provides common functionality
    /// </summary>
    public abstract class BaseEntityExtractor : IEntityExtractor
    {
        private readonly ILogger _logger;
        
        /// <summary>
        /// Constructor for BaseEntityExtractor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        protected BaseEntityExtractor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Extracts entities from the provided text content
        /// </summary>
        /// <param name="content">The text content to extract entities from</param>
        /// <param name="sourceId">Identifier of the source document or data</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of extracted entities</returns>
        public abstract Task<IEnumerable<Entity>> ExtractEntitiesAsync(
            string content, 
            string sourceId,
            string tenantId,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Extracts entities from the provided structured data
        /// </summary>
        /// <param name="data">Dictionary containing structured data field-value pairs</param>
        /// <param name="sourceId">Identifier of the source document or data</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of extracted entities</returns>
        public virtual async Task<IEnumerable<Entity>> ExtractEntitiesFromStructuredDataAsync(
            IDictionary<string, object> data,
            string sourceId,
            string tenantId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing structured data for entity extraction");
            
            if (data == null || !data.Any())
            {
                _logger.LogWarning("Empty structured data provided for extraction");
                return Enumerable.Empty<Entity>();
            }
            
            // Default implementation: convert structured data to text and use the text extraction
            var contentBuilder = new System.Text.StringBuilder();
            
            foreach (var kvp in data)
            {
                if (kvp.Value != null)
                {
                    contentBuilder.AppendLine($"{kvp.Key}: {kvp.Value}");
                }
            }
            
            return await ExtractEntitiesAsync(contentBuilder.ToString(), sourceId, tenantId, cancellationToken);
        }
        
        /// <summary>
        /// Gets the entity types supported by this extractor
        /// </summary>
        /// <returns>A collection of entity types this extractor can identify</returns>
        public abstract IEnumerable<EntityType> GetSupportedEntityTypes();
        
        /// <summary>
        /// Creates a new entity with basic properties set
        /// </summary>
        /// <param name="name">The name or value of the entity</param>
        /// <param name="type">The entity type</param>
        /// <param name="sourceId">The source identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="confidenceScore">The confidence score (0.0 to 1.0)</param>
        /// <returns>A new entity instance</returns>
        protected Entity CreateEntity(
            string name,
            EntityType type,
            string sourceId,
            string tenantId,
            double confidenceScore = 1.0)
        {
            return new Entity
            {
                Name = name,
                Type = type,
                SourceId = sourceId,
                TenantId = tenantId,
                ConfidenceScore = confidenceScore,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// Creates an entity from structured data field
        /// </summary>
        /// <param name="fieldName">The field name</param>
        /// <param name="fieldValue">The field value</param>
        /// <param name="type">The entity type</param>
        /// <param name="sourceId">The source identifier</param>
        /// <param name="tenantId">The tenant identifier</param>
        /// <param name="confidenceScore">The confidence score (0.0 to 1.0)</param>
        /// <returns>A new entity instance or null if value is empty or null</returns>
        protected Entity CreateEntityFromField(
            string fieldName,
            object fieldValue,
            EntityType type,
            string sourceId,
            string tenantId,
            double confidenceScore = 1.0)
        {
            if (fieldValue == null)
                return null;
                
            var stringValue = fieldValue.ToString();
            
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;
                
            var entity = CreateEntity(stringValue, type, sourceId, tenantId, confidenceScore);
            entity.Attributes["FieldName"] = fieldName;
            
            return entity;
        }
        
        /// <summary>
        /// Logs extraction information
        /// </summary>
        /// <param name="message">The log message</param>
        /// <param name="args">The message arguments</param>
        protected void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }
        
        /// <summary>
        /// Logs extraction warnings
        /// </summary>
        /// <param name="message">The log message</param>
        /// <param name="args">The message arguments</param>
        protected void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }
        
        /// <summary>
        /// Logs extraction errors
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <param name="message">The log message</param>
        /// <param name="args">The message arguments</param>
        protected void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, message, args);
        }
    }
} 