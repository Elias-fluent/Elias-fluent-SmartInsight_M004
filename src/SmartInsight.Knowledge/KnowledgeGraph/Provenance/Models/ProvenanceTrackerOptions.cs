namespace SmartInsight.Knowledge.KnowledgeGraph.Provenance.Models
{
    /// <summary>
    /// Configuration options for the provenance tracking system
    /// </summary>
    public class ProvenanceTrackerOptions
    {
        /// <summary>
        /// Whether to enable automatic provenance tracking (default: true)
        /// </summary>
        public bool EnableAutoTracking { get; set; } = true;
        
        /// <summary>
        /// Whether to track dependencies between knowledge elements (default: true)
        /// </summary>
        public bool TrackDependencies { get; set; } = true;
        
        /// <summary>
        /// Maximum depth for dependency tracking (default: 5)
        /// </summary>
        public int MaxDependencyDepth { get; set; } = 5;
        
        /// <summary>
        /// Whether to store the full text context in provenance records (default: true)
        /// </summary>
        public bool StoreFullTextContext { get; set; } = true;
        
        /// <summary>
        /// Maximum length of text context to store (default: 1000 characters)
        /// </summary>
        public int MaxTextContextLength { get; set; } = 1000;
        
        /// <summary>
        /// Default confidence threshold for included provenance (default: 0.3)
        /// </summary>
        public double DefaultConfidenceThreshold { get; set; } = 0.3;
        
        /// <summary>
        /// Whether to include system-generated elements in provenance tracking (default: true)
        /// </summary>
        public bool TrackSystemElements { get; set; } = true;
        
        /// <summary>
        /// Whether to enable provenance validation (default: true)
        /// </summary>
        public bool EnableValidation { get; set; } = true;
        
        /// <summary>
        /// Maximum number of attributes to store in a provenance record (default: 50)
        /// </summary>
        public int MaxAttributeCount { get; set; } = 50;
    }
} 