namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Configuration options for the Triple Store
    /// </summary>
    public class TripleStoreOptions
    {
        /// <summary>
        /// The connection string for the triple store database
        /// </summary>
        public string ConnectionString { get; set; }
        
        /// <summary>
        /// The type of triple store to use
        /// </summary>
        public TripleStoreType StoreType { get; set; } = TripleStoreType.InMemory;
        
        /// <summary>
        /// Whether to enable inference (reasoning) in the triple store
        /// </summary>
        public bool EnableInference { get; set; } = false;
        
        /// <summary>
        /// The default named graph URI to use when not specified
        /// </summary>
        public string DefaultGraphUri { get; set; } = "http://smartinsight.com/graph/default";
        
        /// <summary>
        /// The maximum number of triples to return in a query by default
        /// </summary>
        public int DefaultQueryLimit { get; set; } = 1000;
        
        /// <summary>
        /// The timeout in seconds for SPARQL queries
        /// </summary>
        public int QueryTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Whether to validate triples before inserting them
        /// </summary>
        public bool ValidateTriples { get; set; } = true;
        
        /// <summary>
        /// Minimum confidence threshold for triple queries (0.0 to 1.0)
        /// </summary>
        public double MinConfidenceThreshold { get; set; } = 0.5;
    }
    
    /// <summary>
    /// Types of triple stores supported by the application
    /// </summary>
    public enum TripleStoreType
    {
        /// <summary>
        /// In-memory triple store for development and testing
        /// </summary>
        InMemory,
        
        /// <summary>
        /// File-based RDF store using dotNetRDF
        /// </summary>
        FileStore,
        
        /// <summary>
        /// Triple store in a SQL database
        /// </summary>
        SqlStore,
        
        /// <summary>
        /// Triple store in a NoSQL database
        /// </summary>
        NoSqlStore,
        
        /// <summary>
        /// External RDF database via SPARQL endpoint
        /// </summary>
        SparqlEndpoint
    }
} 