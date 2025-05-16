using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Extractors
{
    /// <summary>
    /// Entity extractor that uses dictionaries of terms to identify entities
    /// </summary>
    public class DictionaryEntityExtractor : BaseEntityExtractor
    {
        private readonly Dictionary<EntityType, Dictionary<string, double>> _dictionaries;
        private readonly StringComparison _comparisonType;
        
        /// <summary>
        /// Initializes a new instance of the DictionaryEntityExtractor class
        /// </summary>
        /// <param name="logger">The logger instance</param>
        /// <param name="caseSensitive">Whether the dictionary matching should be case-sensitive</param>
        public DictionaryEntityExtractor(
            ILogger<DictionaryEntityExtractor> logger,
            bool caseSensitive = false) : base(logger)
        {
            _dictionaries = new Dictionary<EntityType, Dictionary<string, double>>();
            _comparisonType = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            InitializeDefaultDictionaries();
        }
        
        /// <summary>
        /// Extracts entities from the provided text content using dictionary matches
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
            LogInformation("Extracting entities using dictionary matches from content");
            
            if (string.IsNullOrEmpty(content))
            {
                LogWarning("Empty content provided for extraction");
                return Task.FromResult(Enumerable.Empty<Entity>());
            }
            
            var entities = new List<Entity>();
            var words = content.Split(new[] { ' ', '\t', '\n', '\r', ',', ';', '.', ':', '!', '?', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '"', '\'' }, 
                StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var entry in _dictionaries)
            {
                var entityType = entry.Key;
                var dictionary = entry.Value;
                
                // Check for single-word matches
                foreach (var word in words)
                {
                    if (dictionary.TryGetValue(GetComparisonKey(word), out var confidence))
                    {
                        var entity = CreateEntity(word, entityType, sourceId, tenantId, confidence);
                        entities.Add(entity);
                    }
                }
                
                // Check for multi-word matches
                foreach (var term in dictionary.Keys)
                {
                    if (term.Contains(" "))
                    {
                        int index = -1;
                        while ((index = FindStringIgnoreCase(content, term, index + 1, _comparisonType)) >= 0)
                        {
                            dictionary.TryGetValue(GetComparisonKey(term), out var confidence);
                            var entity = CreateEntity(term, entityType, sourceId, tenantId, confidence);
                            entity.StartPosition = index;
                            entity.EndPosition = index + term.Length;
                            entity.OriginalContext = GetContext(content, index, term.Length);
                            entities.Add(entity);
                        }
                    }
                }
            }
            
            LogInformation("Extracted {EntityCount} entities using dictionary matches", entities.Count);
            return Task.FromResult(entities.AsEnumerable());
        }
        
        /// <summary>
        /// Gets the entity types supported by this extractor
        /// </summary>
        /// <returns>A collection of entity types this extractor can identify</returns>
        public override IEnumerable<EntityType> GetSupportedEntityTypes()
        {
            return _dictionaries.Keys;
        }
        
        /// <summary>
        /// Adds a term to the dictionary for a specific entity type
        /// </summary>
        /// <param name="entityType">The entity type</param>
        /// <param name="term">The term to add</param>
        /// <param name="confidence">Confidence score for matches (0.0 to 1.0)</param>
        public void AddTerm(EntityType entityType, string term, double confidence = 1.0)
        {
            if (string.IsNullOrEmpty(term))
                throw new ArgumentNullException(nameof(term));
                
            if (!_dictionaries.TryGetValue(entityType, out var dictionary))
            {
                dictionary = new Dictionary<string, double>(
                    _comparisonType == StringComparison.Ordinal
                        ? StringComparer.Ordinal
                        : StringComparer.OrdinalIgnoreCase);
                _dictionaries[entityType] = dictionary;
            }
            
            dictionary[term] = Math.Clamp(confidence, 0.0, 1.0);
            LogInformation("Added term '{Term}' for entity type {EntityType} with confidence {Confidence}", 
                term, entityType, confidence);
        }
        
        /// <summary>
        /// Adds multiple terms to the dictionary for a specific entity type
        /// </summary>
        /// <param name="entityType">The entity type</param>
        /// <param name="terms">The terms to add</param>
        /// <param name="confidence">Confidence score for matches (0.0 to 1.0)</param>
        public void AddTerms(EntityType entityType, IEnumerable<string> terms, double confidence = 1.0)
        {
            if (terms == null)
                throw new ArgumentNullException(nameof(terms));
                
            foreach (var term in terms)
            {
                if (!string.IsNullOrWhiteSpace(term))
                {
                    AddTerm(entityType, term, confidence);
                }
            }
        }
        
        /// <summary>
        /// Gets a key suitable for the configured comparison type
        /// </summary>
        /// <param name="text">The text to convert</param>
        /// <returns>The comparison key</returns>
        private string GetComparisonKey(string text)
        {
            if (_comparisonType == StringComparison.OrdinalIgnoreCase)
                return text.ToLowerInvariant();
                
            return text;
        }
        
        /// <summary>
        /// Finds a string within another string with specified comparison options
        /// </summary>
        /// <param name="source">The source string</param>
        /// <param name="value">The string to find</param>
        /// <param name="startIndex">The starting index</param>
        /// <param name="comparison">The string comparison type</param>
        /// <returns>The index of the found string or -1 if not found</returns>
        private static int FindStringIgnoreCase(string source, string value, int startIndex, StringComparison comparison)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value) || startIndex >= source.Length)
                return -1;
                
            return source.IndexOf(value, startIndex, comparison);
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
        /// Initializes default dictionaries for common entity types
        /// </summary>
        private void InitializeDefaultDictionaries()
        {
            // Organization examples
            AddTerms(EntityType.Organization, new[]
            {
                "Microsoft", "Google", "Apple", "Amazon", "Facebook", "Tesla", "IBM",
                "Intel", "Oracle", "Salesforce", "Adobe", "Netflix", "Spotify",
                "LinkedIn", "Twitter", "Uber", "Airbnb", "eBay", "PayPal", "Slack",
                "Zoom", "GitLab", "GitHub", "Atlassian", "JIRA", "Confluence", "Trello"
            }, 0.9);
            
            // Technical terms
            AddTerms(EntityType.TechnicalTerm, new[]
            {
                "API", "REST", "GraphQL", "SQL", "HTTP", "HTTPS", "TCP", "UDP", "IP",
                "OAuth", "JWT", "JSON", "XML", "YAML", "HTML", "CSS", "JavaScript",
                "TypeScript", "Python", "Java", "C#", "C++", "Go", "Rust", "Kotlin",
                "Swift", "Docker", "Kubernetes", "Microservice", "Serverless",
                "Machine Learning", "Artificial Intelligence", "Data Science",
                "Big Data", "Cloud Computing", "DevOps", "CI/CD", "Git", "CRUD",
                "Database", "NoSQL", "PostgreSQL", "MySQL", "MongoDB", "Redis"
            }, 0.8);
            
            // Job titles
            AddTerms(EntityType.JobTitle, new[]
            {
                "CEO", "CTO", "CFO", "COO", "CIO", "CMO", "CISO",
                "Director", "Manager", "VP", "Vice President", "SVP", "EVP",
                "Software Engineer", "Data Scientist", "Product Manager",
                "Project Manager", "UX Designer", "UI Designer", "DevOps Engineer",
                "Systems Administrator", "Database Administrator", "Network Engineer",
                "Security Engineer", "QA Engineer", "Tester", "Technical Writer",
                "Scrum Master", "Agile Coach", "Tech Lead", "Team Lead",
                "Principal Engineer", "Senior Engineer", "Junior Engineer"
            }, 0.8);
            
            // Skills
            AddTerms(EntityType.Skill, new[]
            {
                "Programming", "Coding", "Development", "Testing", "Debugging",
                "Web Development", "Mobile Development", "Backend", "Frontend",
                "Full Stack", "Database Design", "System Architecture",
                "Cloud Architecture", "Security Analysis", "Network Administration",
                "Project Management", "Technical Writing", "Data Analysis",
                "Business Intelligence", "Machine Learning", "Natural Language Processing",
                "Computer Vision", "DevOps", "CI/CD", "Version Control", "Git",
                "Agile", "Scrum", "Kanban", "Leadership", "Team Management"
            }, 0.7);
            
            // Database terms
            AddTerms(EntityType.DatabaseTable, new[]
            {
                "Users", "Customers", "Products", "Orders", "Payments",
                "Transactions", "Accounts", "Profiles", "Sessions", "Logs",
                "Configurations", "Settings", "Permissions", "Roles", "Groups",
                "Departments", "Categories", "Tags", "Comments", "Reviews",
                "Ratings", "Metrics", "Analytics", "Reports", "Audits",
                "Inventory", "Subscriptions", "Plans", "Features", "Pricing"
            }, 0.6);
            
            // Database columns
            AddTerms(EntityType.DatabaseColumn, new[]
            {
                "id", "name", "email", "phone", "address", "city", "state",
                "country", "zip", "postal_code", "created_at", "updated_at",
                "deleted_at", "status", "type", "category", "description",
                "price", "cost", "quantity", "user_id", "customer_id",
                "order_id", "product_id", "payment_id", "transaction_id",
                "active", "enabled", "verified", "password", "token"
            }, 0.6);
        }
    }
} 