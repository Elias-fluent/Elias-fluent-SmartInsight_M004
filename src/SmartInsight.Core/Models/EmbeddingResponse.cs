using System.Collections.Generic;

namespace SmartInsight.Core.Models
{
    /// <summary>
    /// Response model for embedding generation
    /// </summary>
    public class EmbeddingResponse
    {
        /// <summary>
        /// The vector embedding
        /// </summary>
        public List<float> Embedding { get; set; } = new List<float>();
        
        /// <summary>
        /// The original text that was embedded
        /// </summary>
        public string? OriginalText { get; set; }
        
        /// <summary>
        /// The model used for embedding
        /// </summary>
        public string? ModelName { get; set; }
        
        /// <summary>
        /// Total tokens processed
        /// </summary>
        public int TotalTokens { get; set; }
        
        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }
} 