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
        /// Threshold for considering a match to be ambiguous (0.0 to 1.0).
        /// If the difference between top match and second match is less than this, it's considered ambiguous.
        /// </summary>
        public double AmbiguityThreshold { get; set; } = 0.1;
        
        /// <summary>
        /// Maximum number of matches to return
        /// </summary>
        public int MaxMatches { get; set; } = 3;

        /// <summary>
        /// Weight of semantic similarity in the overall confidence score (0.0 to 1.0)
        /// </summary>
        public double SemanticSimilarityWeight { get; set; } = 0.6;

        /// <summary>
        /// Weight of context relevance in the overall confidence score (0.0 to 1.0)
        /// </summary>
        public double ContextRelevanceWeight { get; set; } = 0.2;

        /// <summary>
        /// Weight of historical accuracy in the overall confidence score (0.0 to 1.0)
        /// </summary>
        public double HistoricalAccuracyWeight { get; set; } = 0.2;

        /// <summary>
        /// Number of historical interactions to consider for historical confidence calculation
        /// </summary>
        public int HistoricalInteractionsCount { get; set; } = 10;

        /// <summary>
        /// Threshold for triggering clarification requests (0.0 to 1.0)
        /// </summary>
        public double ClarificationThreshold { get; set; } = 0.5;

        /// <summary>
        /// Threshold below which intent is considered a mismatch and fallback should be used (0.0 to 1.0)
        /// </summary>
        public double MismatchThreshold { get; set; } = 0.3;
        
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

        /// <summary>
        /// Whether to enable contextual confidence boosting based on conversation history
        /// </summary>
        public bool EnableContextualConfidence { get; set; } = true;

        /// <summary>
        /// Boost factor for intents that match previously detected intents in context (0.0 to 0.5)
        /// </summary>
        public double ContextualBoostFactor { get; set; } = 0.1;
    }
} 