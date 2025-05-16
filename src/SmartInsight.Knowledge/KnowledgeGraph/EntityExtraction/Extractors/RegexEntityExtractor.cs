using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Extractors
{
    /// <summary>
    /// Entity extractor that uses regular expressions to identify entities
    /// </summary>
    public class RegexEntityExtractor : BaseEntityExtractor
    {
        private readonly Dictionary<EntityType, List<(string Name, Regex Pattern)>> _patterns;
        
        /// <summary>
        /// Initializes a new instance of the RegexEntityExtractor class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public RegexEntityExtractor(ILogger<RegexEntityExtractor> logger) : base(logger)
        {
            _patterns = new Dictionary<EntityType, List<(string, Regex)>>();
            InitializeDefaultPatterns();
        }
        
        /// <summary>
        /// Extracts entities from the provided text content using regular expressions
        /// </summary>
        /// <param name="content">The text content to extract entities from</param>
        /// <param name="sourceId">Identifier of the source document or data</param>
        /// <param name="tenantId">The tenant ID for multi-tenant isolation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A collection of extracted entities</returns>
        public override Task<IEnumerable<Entity>> ExtractEntitiesAsync(
            string content, 
            string sourceId, 
            string tenantId, 
            CancellationToken cancellationToken = default)
        {
            LogInformation("Extracting entities using regex patterns from content");
            
            if (string.IsNullOrEmpty(content))
            {
                LogWarning("Empty content provided for extraction");
                return Task.FromResult(Enumerable.Empty<Entity>());
            }
            
            var entities = new List<Entity>();
            
            // Process each entity type and its regex patterns
            foreach (var typePatterns in _patterns)
            {
                var entityType = typePatterns.Key;
                var patterns = typePatterns.Value;
                
                foreach (var pattern in patterns)
                {
                    try
                    {
                        var matches = pattern.Pattern.Matches(content);
                        
                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                var entity = CreateEntity(
                                    match.Value,
                                    entityType,
                                    sourceId,
                                    tenantId,
                                    0.9); // High confidence for regex matches
                                    
                                entity.OriginalContext = GetContext(content, match.Index, match.Length);
                                entity.StartPosition = match.Index;
                                entity.EndPosition = match.Index + match.Length;
                                entity.Attributes["PatternName"] = pattern.Name;
                                
                                // For named captures, add them as attributes
                                foreach (var groupName in pattern.Pattern.GetGroupNames()
                                    .Where(name => !int.TryParse(name, out _)))  // Skip numeric groups
                                {
                                    if (match.Groups[groupName].Success)
                                    {
                                        entity.Attributes[groupName] = match.Groups[groupName].Value;
                                    }
                                }
                                
                                entities.Add(entity);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Error extracting entities using pattern {PatternName}: {ErrorMessage}",
                            pattern.Name, ex.Message);
                    }
                }
            }
            
            LogInformation("Extracted {EntityCount} entities using regex patterns", entities.Count);
            return Task.FromResult(entities.AsEnumerable());
        }
        
        /// <summary>
        /// Gets the entity types supported by this extractor
        /// </summary>
        /// <returns>A collection of entity types this extractor can identify</returns>
        public override IEnumerable<EntityType> GetSupportedEntityTypes()
        {
            return _patterns.Keys;
        }
        
        /// <summary>
        /// Adds a new regex pattern for extracting entities of a specific type
        /// </summary>
        /// <param name="entityType">The entity type</param>
        /// <param name="patternName">A name for this pattern</param>
        /// <param name="pattern">The regex pattern</param>
        /// <param name="options">Regex options</param>
        public void AddPattern(EntityType entityType, string patternName, string pattern, RegexOptions options = RegexOptions.None)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentNullException(nameof(pattern));
                
            if (string.IsNullOrEmpty(patternName))
                throw new ArgumentNullException(nameof(patternName));
                
            try
            {
                var regex = new Regex(pattern, options);
                
                if (!_patterns.TryGetValue(entityType, out var patterns))
                {
                    patterns = new List<(string, Regex)>();
                    _patterns[entityType] = patterns;
                }
                
                patterns.Add((patternName, regex));
                LogInformation("Added pattern {PatternName} for entity type {EntityType}", patternName, entityType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid regex pattern '{pattern}': {ex.Message}", nameof(pattern), ex);
            }
        }
        
        /// <summary>
        /// Gets a substring of the original content surrounding the match for context
        /// </summary>
        /// <param name="content">The original content</param>
        /// <param name="matchIndex">The start index of the match</param>
        /// <param name="matchLength">The length of the match</param>
        /// <param name="contextSize">The number of characters to include before and after the match</param>
        /// <returns>The context string</returns>
        private string GetContext(string content, int matchIndex, int matchLength, int contextSize = 100)
        {
            var startIndex = Math.Max(0, matchIndex - contextSize);
            var length = Math.Min(content.Length - startIndex, matchLength + (2 * contextSize));
            
            return content.Substring(startIndex, length);
        }
        
        /// <summary>
        /// Initializes the default regex patterns for common entity types
        /// </summary>
        private void InitializeDefaultPatterns()
        {
            // Email addresses
            AddPattern(
                EntityType.Email,
                "StandardEmail",
                @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
                RegexOptions.IgnoreCase);
                
            // URLs
            AddPattern(
                EntityType.Url,
                "StandardUrl",
                @"(https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|www\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9]+\.[^\s]{2,}|www\.[a-zA-Z0-9]+\.[^\s]{2,})",
                RegexOptions.IgnoreCase);
                
            // Phone numbers (common formats)
            AddPattern(
                EntityType.PhoneNumber,
                "USPhoneNumber",
                @"\b(\+\d{1,2}\s?)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}\b",
                RegexOptions.None);
                
            // Date patterns (various common formats)
            AddPattern(
                EntityType.DateTime,
                "ISODate",
                @"\b\d{4}-\d{2}-\d{2}\b",
                RegexOptions.None);
                
            AddPattern(
                EntityType.DateTime,
                "USDate",
                @"\b\d{1,2}/\d{1,2}/\d{2,4}\b",
                RegexOptions.None);
                
            AddPattern(
                EntityType.DateTime,
                "EUDate",
                @"\b\d{1,2}\.\d{1,2}\.\d{2,4}\b",
                RegexOptions.None);
                
            // Money (USD, EUR, GBP)
            AddPattern(
                EntityType.Money,
                "USD",
                @"\$\s?\d+(?:\.\d{2})?",
                RegexOptions.None);
                
            AddPattern(
                EntityType.Money,
                "EUR",
                @"€\s?\d+(?:,\d{2})?",
                RegexOptions.None);
                
            AddPattern(
                EntityType.Money,
                "GBP",
                @"£\s?\d+(?:\.\d{2})?",
                RegexOptions.None);
                
            // Percentages
            AddPattern(
                EntityType.Percentage,
                "StandardPercentage",
                @"\b\d+(?:\.\d+)?%\b",
                RegexOptions.None);
                
            // Database identifiers
            AddPattern(
                EntityType.DatabaseTable,
                "SQLTableName",
                @"\b(?:from|join|update|into)\s+(?<Table>[a-zA-Z][a-zA-Z0-9_]*)\b",
                RegexOptions.IgnoreCase);
                
            AddPattern(
                EntityType.DatabaseColumn,
                "SQLColumnName",
                @"\b(?:select|where|group\s+by|order\s+by)\s+(?<Column>[a-zA-Z][a-zA-Z0-9_]*)\b",
                RegexOptions.IgnoreCase);
                
            // Code snippets (simplified)
            AddPattern(
                EntityType.CodeSnippet,
                "FunctionDeclaration",
                @"\b(?:function|def|public|private|protected|void|async|static)\s+(?<FunctionName>[a-zA-Z][a-zA-Z0-9_]*)\s*\(",
                RegexOptions.IgnoreCase);
        }
    }
} 