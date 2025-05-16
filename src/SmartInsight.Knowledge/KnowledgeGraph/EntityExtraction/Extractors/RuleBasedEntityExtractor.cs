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
    /// Entity extractor that applies contextual rules to identify entities
    /// </summary>
    public class RuleBasedEntityExtractor : BaseEntityExtractor
    {
        private readonly List<ExtractionRule> _rules = new List<ExtractionRule>();
        
        /// <summary>
        /// Initializes a new instance of the RuleBasedEntityExtractor class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public RuleBasedEntityExtractor(ILogger<RuleBasedEntityExtractor> logger) : base(logger)
        {
            InitializeDefaultRules();
        }
        
        /// <summary>
        /// Rule definition for entity extraction
        /// </summary>
        private class ExtractionRule
        {
            public string Name { get; set; }
            public EntityType EntityType { get; set; }
            public Func<string, List<(string Value, int StartIndex, int Length, Dictionary<string, object> Attributes)>> Matcher { get; set; }
            public double ConfidenceScore { get; set; }
        }
        
        /// <summary>
        /// Extracts entities from the provided text content using contextual rules
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
            LogInformation("Extracting entities using rule-based approach from content");
            
            if (string.IsNullOrEmpty(content))
            {
                LogWarning("Empty content provided for extraction");
                return Task.FromResult(Enumerable.Empty<Entity>());
            }
            
            var entities = new List<Entity>();
            
            foreach (var rule in _rules)
            {
                try
                {
                    var matches = rule.Matcher(content);
                    
                    foreach (var match in matches)
                    {
                        var entity = CreateEntity(match.Value, rule.EntityType, sourceId, tenantId, rule.ConfidenceScore);
                        entity.OriginalContext = GetContext(content, match.StartIndex, match.Length);
                        entity.StartPosition = match.StartIndex;
                        entity.EndPosition = match.StartIndex + match.Length;
                        entity.Attributes["RuleName"] = rule.Name;
                        
                        // Add any additional attributes from the match
                        foreach (var attribute in match.Attributes)
                        {
                            entity.Attributes[attribute.Key] = attribute.Value;
                        }
                        
                        entities.Add(entity);
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error applying rule {RuleName}: {ErrorMessage}", rule.Name, ex.Message);
                }
            }
            
            LogInformation("Extracted {EntityCount} entities using rule-based approach", entities.Count);
            return Task.FromResult(entities.AsEnumerable());
        }
        
        /// <summary>
        /// Gets the entity types supported by this extractor
        /// </summary>
        /// <returns>A collection of entity types this extractor can identify</returns>
        public override IEnumerable<EntityType> GetSupportedEntityTypes()
        {
            return _rules.Select(r => r.EntityType).Distinct();
        }
        
        /// <summary>
        /// Adds a rule for entity extraction
        /// </summary>
        /// <param name="name">Name of the rule</param>
        /// <param name="entityType">Type of entity to extract</param>
        /// <param name="matcher">Function that identifies entities</param>
        /// <param name="confidenceScore">Confidence score for matches (0.0 to 1.0)</param>
        public void AddRule(
            string name,
            EntityType entityType,
            Func<string, List<(string Value, int StartIndex, int Length, Dictionary<string, object> Attributes)>> matcher,
            double confidenceScore = 0.7)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
                
            if (matcher == null)
                throw new ArgumentNullException(nameof(matcher));
                
            _rules.Add(new ExtractionRule
            {
                Name = name,
                EntityType = entityType,
                Matcher = matcher,
                ConfidenceScore = Math.Clamp(confidenceScore, 0.0, 1.0)
            });
            
            LogInformation("Added rule {RuleName} for entity type {EntityType}", name, entityType);
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
        /// Initializes default rules for entity extraction
        /// </summary>
        private void InitializeDefaultRules()
        {
            // Person name rule (simplified: looking for "Mr.", "Ms.", "Dr.", etc. followed by capitalized words)
            AddRule(
                "PersonNameWithTitle",
                EntityType.Person,
                content =>
                {
                    var results = new List<(string, int, int, Dictionary<string, object>)>();
                    var titlePattern = new Regex(@"\b(Mr\.|Mrs\.|Ms\.|Dr\.|Prof\.|Sir|Madam)\s+([A-Z][a-z]+(?:\s+[A-Z][a-z]+){0,2})\b");
                    
                    foreach (Match match in titlePattern.Matches(content))
                    {
                        var title = match.Groups[1].Value;
                        var name = match.Groups[2].Value;
                        var fullName = $"{title} {name}";
                        
                        var attributes = new Dictionary<string, object>
                        {
                            { "Title", title },
                            { "Name", name }
                        };
                        
                        results.Add((fullName, match.Index, match.Length, attributes));
                    }
                    
                    return results;
                },
                0.85);
                
            // Organization name rule (simplified: looking for organization indicators)
            AddRule(
                "OrganizationWithSuffix",
                EntityType.Organization,
                content =>
                {
                    var results = new List<(string, int, int, Dictionary<string, object>)>();
                    var pattern = new Regex(@"\b([A-Z][a-zA-Z0-9]+(?:\s+[A-Z][a-zA-Z0-9]+){0,5})\s+(Inc\.|Corp\.|LLC|Ltd\.|Limited|Corporation|Company|GmbH|Co\.|Group|Holdings)\b");
                    
                    foreach (Match match in pattern.Matches(content))
                    {
                        var name = match.Groups[1].Value;
                        var suffix = match.Groups[2].Value;
                        var fullName = $"{name} {suffix}";
                        
                        var attributes = new Dictionary<string, object>
                        {
                            { "Name", name },
                            { "Suffix", suffix }
                        };
                        
                        results.Add((fullName, match.Index, match.Length, attributes));
                    }
                    
                    return results;
                },
                0.9);
                
            // Location rule (simplified: looking for location indicators)
            AddRule(
                "LocationWithPrefix",
                EntityType.Location,
                content =>
                {
                    var results = new List<(string, int, int, Dictionary<string, object>)>();
                    var pattern = new Regex(@"\b(in|at|from|to|near|around)\s+([A-Z][a-zA-Z]+(?:\s+[A-Z][a-zA-Z]+){0,2})\b");
                    
                    foreach (Match match in pattern.Matches(content))
                    {
                        var prefix = match.Groups[1].Value;
                        var location = match.Groups[2].Value;
                        
                        var attributes = new Dictionary<string, object>
                        {
                            { "Prefix", prefix }
                        };
                        
                        results.Add((location, match.Index + prefix.Length + 1, location.Length, attributes));
                    }
                    
                    return results;
                },
                0.6); // Lower confidence since this is a heuristic
                
            // Product with version rule
            AddRule(
                "ProductWithVersion",
                EntityType.Product,
                content =>
                {
                    var results = new List<(string, int, int, Dictionary<string, object>)>();
                    var pattern = new Regex(@"\b([A-Z][a-zA-Z0-9]+(?:\s+[A-Z][a-zA-Z0-9]+){0,3})\s+(v?[0-9]+(?:\.[0-9]+){1,3})\b");
                    
                    foreach (Match match in pattern.Matches(content))
                    {
                        var product = match.Groups[1].Value;
                        var version = match.Groups[2].Value;
                        var fullProduct = $"{product} {version}";
                        
                        var attributes = new Dictionary<string, object>
                        {
                            { "ProductName", product },
                            { "Version", version }
                        };
                        
                        results.Add((fullProduct, match.Index, match.Length, attributes));
                    }
                    
                    return results;
                },
                0.85);
                
            // API endpoint rule
            AddRule(
                "ApiEndpoint",
                EntityType.Api,
                content =>
                {
                    var results = new List<(string, int, int, Dictionary<string, object>)>();
                    var pattern = new Regex(@"\b(GET|POST|PUT|DELETE|PATCH)\s+(/api/[a-zA-Z0-9/\-_{}]+)\b");
                    
                    foreach (Match match in pattern.Matches(content))
                    {
                        var method = match.Groups[1].Value;
                        var endpoint = match.Groups[2].Value;
                        var fullEndpoint = $"{method} {endpoint}";
                        
                        var attributes = new Dictionary<string, object>
                        {
                            { "Method", method },
                            { "Endpoint", endpoint }
                        };
                        
                        results.Add((fullEndpoint, match.Index, match.Length, attributes));
                    }
                    
                    return results;
                },
                0.9);
        }
    }
} 