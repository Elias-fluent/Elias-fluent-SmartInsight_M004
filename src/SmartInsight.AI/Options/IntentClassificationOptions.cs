namespace SmartInsight.AI.Options
{
    /// <summary>
    /// Options for configuring intent classification behavior
    /// </summary>
    public class IntentClassificationOptions
    {
        /// <summary>
        /// Model name to use for generating embeddings
        /// </summary>
        public string EmbeddingModelName { get; set; } = "llama3";
        
        /// <summary>
        /// Similarity threshold for considering a match (0.0 to 1.0)
        /// </summary>
        public double SimilarityThreshold { get; set; } = 0.7;
        
        /// <summary>
        /// Threshold for considering a match to be high confidence (0.0 to 1.0)
        /// </summary>
        public double HighConfidenceThreshold { get; set; } = 0.85;
        
        /// <summary>
        /// Maximum number of matches to return
        /// </summary>
        public int MaxMatches { get; set; } = 3;
        
        /// <summary>
        /// Path to the file for storing/loading intent definitions
        /// </summary>
        public string? IntentDefinitionsFilePath { get; set; }
        
        /// <summary>
        /// Whether to persist intent definitions to disk
        /// </summary>
        public bool PersistIntentDefinitions { get; set; } = true;
        
        /// <summary>
        /// Whether to load intent definitions from disk on startup
        /// </summary>
        public bool LoadIntentDefinitionsOnStartup { get; set; } = true;
    }
} 