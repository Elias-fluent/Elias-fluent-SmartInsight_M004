namespace SmartInsight.Knowledge.VectorDb.Embeddings
{
    /// <summary>
    /// Configuration options for vector embedding generation
    /// </summary>
    public class EmbeddingOptions
    {
        /// <summary>
        /// Default model to use for embeddings
        /// </summary>
        public string DefaultModel { get; set; } = "llama3";
        
        /// <summary>
        /// Maximum characters to process in a single embedding request
        /// </summary>
        public int MaxInputLength { get; set; } = 8192;
        
        /// <summary>
        /// Default chunk size for text chunking (characters)
        /// </summary>
        public int DefaultChunkSize { get; set; } = 1000;
        
        /// <summary>
        /// Default overlap between chunks (characters)
        /// </summary>
        public int DefaultChunkOverlap { get; set; } = 200;
        
        /// <summary>
        /// Maximum batch size for bulk embedding requests
        /// </summary>
        public int MaxBatchSize { get; set; } = 32;
        
        /// <summary>
        /// Retry attempts for failed embedding requests
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>
        /// Delay in milliseconds between retry attempts
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;
        
        /// <summary>
        /// Whether to normalize embedding vectors
        /// </summary>
        public bool NormalizeVectors { get; set; } = true;
        
        /// <summary>
        /// Name of the collection to store document embeddings
        /// </summary>
        public string DocumentCollection { get; set; } = "documents";
        
        /// <summary>
        /// Model-specific options for embeddings
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> ModelOptions { get; set; } = 
            new Dictionary<string, Dictionary<string, object>>();
    }
} 