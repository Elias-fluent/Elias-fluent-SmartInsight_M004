using System.Collections.Generic;

namespace SmartInsight.Core.Models
{
    /// <summary>
    /// Options for embedding generation
    /// </summary>
    public class EmbeddingOptions
    {
        /// <summary>
        /// Default model to use for embeddings if none specified
        /// </summary>
        public string DefaultModel { get; set; } = "llama3";
        
        /// <summary>
        /// Maximum length of text to embed, excess is truncated
        /// </summary>
        public int MaxInputLength { get; set; } = 8192;
        
        /// <summary>
        /// Maximum batch size for batch operations
        /// </summary>
        public int MaxBatchSize { get; set; } = 16;
        
        /// <summary>
        /// Whether to normalize vectors to unit length (L2 norm)
        /// </summary>
        public bool NormalizeVectors { get; set; } = true;
        
        /// <summary>
        /// Model-specific parameters keyed by model name
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> ModelOptions { get; set; } = new Dictionary<string, Dictionary<string, object>>();
    }
} 