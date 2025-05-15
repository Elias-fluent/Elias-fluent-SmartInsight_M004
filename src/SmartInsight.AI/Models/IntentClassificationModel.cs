using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartInsight.AI.Models
{
    /// <summary>
    /// Represents an intent with its associated patterns, examples, and embedding vectors
    /// </summary>
    public class IntentDefinition
    {
        /// <summary>
        /// The unique name of the intent
        /// </summary>
        public string Name { get; set; } = null!;
        
        /// <summary>
        /// Optional description of what this intent represents
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Example queries that should trigger this intent
        /// </summary>
        public List<string> Examples { get; set; } = new List<string>();
        
        /// <summary>
        /// Collection of embedding vectors for the examples
        /// </summary>
        [JsonIgnore]
        public List<List<float>> ExampleEmbeddings { get; set; } = new List<List<float>>();
        
        /// <summary>
        /// Entity slots that this intent can extract from queries
        /// </summary>
        public List<EntitySlot> EntitySlots { get; set; } = new List<EntitySlot>();
        
        /// <summary>
        /// Parent intent in a hierarchical structure (null if top-level)
        /// </summary>
        public string? ParentIntent { get; set; }
        
        /// <summary>
        /// Child intents in a hierarchical structure
        /// </summary>
        public List<string> ChildIntents { get; set; } = new List<string>();
        
        /// <summary>
        /// Custom parameters for intent behavior
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Adds a new example and optionally its embedding
        /// </summary>
        public void AddExample(string example, List<float>? embedding = null)
        {
            if (string.IsNullOrWhiteSpace(example))
            {
                throw new ArgumentException("Example cannot be empty", nameof(example));
            }
            
            Examples.Add(example);
            
            if (embedding != null)
            {
                ExampleEmbeddings.Add(embedding);
            }
        }
    }
    
    /// <summary>
    /// Represents an entity slot in an intent that can be filled from a user query
    /// </summary>
    public class EntitySlot
    {
        /// <summary>
        /// The name of the entity slot
        /// </summary>
        public string Name { get; set; } = null!;
        
        /// <summary>
        /// The entity type that can fill this slot
        /// </summary>
        public string EntityType { get; set; } = null!;
        
        /// <summary>
        /// Whether this slot is required for the intent to be valid
        /// </summary>
        public bool Required { get; set; }
        
        /// <summary>
        /// Default value if the slot is not filled
        /// </summary>
        public string? DefaultValue { get; set; }
        
        /// <summary>
        /// Prompt templates for extracting this entity
        /// </summary>
        public List<string> ExtractionPrompts { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Model for classifying intents using vector embeddings and similarity matching
    /// </summary>
    public class IntentClassificationModel
    {
        /// <summary>
        /// Collection of intent definitions
        /// </summary>
        public Dictionary<string, IntentDefinition> Intents { get; set; } = new Dictionary<string, IntentDefinition>();
        
        /// <summary>
        /// Maps alternate phrases to canonical intent names
        /// </summary>
        public Dictionary<string, string> IntentAliases { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// The embedding model to use for generating vectors
        /// </summary>
        public string EmbeddingModel { get; set; } = "llama3";
        
        /// <summary>
        /// The similarity threshold for matching intents (0.0 to 1.0)
        /// </summary>
        public double SimilarityThreshold { get; set; } = 0.7;
        
        /// <summary>
        /// Adds a new intent definition
        /// </summary>
        public void AddIntent(IntentDefinition intent)
        {
            if (intent == null)
            {
                throw new ArgumentNullException(nameof(intent));
            }
            
            if (string.IsNullOrWhiteSpace(intent.Name))
            {
                throw new ArgumentException("Intent name cannot be empty", nameof(intent));
            }
            
            Intents[intent.Name] = intent;
        }
        
        /// <summary>
        /// Adds an alias for an intent
        /// </summary>
        public void AddIntentAlias(string alias, string intentName)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                throw new ArgumentException("Alias cannot be empty", nameof(alias));
            }
            
            if (string.IsNullOrWhiteSpace(intentName))
            {
                throw new ArgumentException("Intent name cannot be empty", nameof(intentName));
            }
            
            if (!Intents.ContainsKey(intentName))
            {
                throw new ArgumentException($"Intent '{intentName}' does not exist", nameof(intentName));
            }
            
            IntentAliases[alias] = intentName;
        }
        
        /// <summary>
        /// Resolves an intent name from its canonical name or an alias
        /// </summary>
        public string ResolveIntentName(string intentNameOrAlias)
        {
            if (Intents.ContainsKey(intentNameOrAlias))
            {
                return intentNameOrAlias;
            }
            
            if (IntentAliases.TryGetValue(intentNameOrAlias, out var intentName))
            {
                return intentName;
            }
            
            throw new ArgumentException($"Intent or alias '{intentNameOrAlias}' not found");
        }
        
        /// <summary>
        /// Calculates cosine similarity between two embedding vectors
        /// </summary>
        public static double CalculateCosineSimilarity(List<float> vectorA, List<float> vectorB)
        {
            if (vectorA.Count != vectorB.Count)
            {
                throw new ArgumentException("Vector dimensions must match");
            }
            
            double dotProduct = 0;
            double normA = 0;
            double normB = 0;
            
            for (int i = 0; i < vectorA.Count; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }
            
            if (normA == 0 || normB == 0)
            {
                return 0;
            }
            
            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }
} 