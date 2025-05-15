/**
 * Qdrant Initialization Script
 * 
 * This script sets up the initial collections in Qdrant with tenant namespaces
 * for proper multi-tenant isolation.
 */

// Using fetch for HTTP requests - script can be run with Node.js or a browser
async function setupQdrant(baseUrl = 'http://localhost:6333') {
    console.log('Starting Qdrant initialization with tenant namespaces...');

    // Collection configurations with standard vector dimensions
    const TEXT_VECTOR_SIZE = 1536; // OpenAI embeddings dimension
    const SEARCH_VECTOR_SIZE = 768; // Smaller dimension for search vectors

    const collections = [
        {
            name: 'documents',
            description: 'Document embeddings for knowledge retrieval',
            vectorSize: TEXT_VECTOR_SIZE
        },
        {
            name: 'entities',
            description: 'Entity embeddings for the knowledge graph',
            vectorSize: TEXT_VECTOR_SIZE
        },
        {
            name: 'search',
            description: 'Optimized vectors for search functionality',
            vectorSize: SEARCH_VECTOR_SIZE
        }
    ];

    // Example tenant IDs (in production these would be retrieved from database)
    const tenants = [
        { id: 'default', name: 'Default Tenant' }
    ];

    // Create each collection
    for (const collection of collections) {
        try {
            console.log(`Creating collection: ${collection.name}`);
            
            // Check if collection exists
            const checkResponse = await fetch(`${baseUrl}/collections/${collection.name}`);
            
            if (checkResponse.status === 404) {
                // Collection doesn't exist, create it
                const createResponse = await fetch(`${baseUrl}/collections/${collection.name}`, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        vectors: {
                            size: collection.vectorSize,
                            distance: 'Cosine'
                        },
                        optimizers_config: {
                            default_segment_number: 2
                        },
                        replication_factor: 1,
                        write_consistency_factor: 1,
                        on_disk_payload: true,
                        hnsw_config: {
                            m: 16,
                            ef_construct: 100,
                            full_scan_threshold: 10000
                        },
                        wal_config: {
                            wal_capacity_mb: 32,
                            wal_segments_ahead: 2
                        },
                        quantization_config: null,
                        sharding_config: {
                            shard_number: 1
                        },
                        sparse_vectors: null
                    })
                });
                
                if (createResponse.ok) {
                    console.log(`Created collection: ${collection.name}`);
                } else {
                    console.error(`Failed to create collection ${collection.name}:`, await createResponse.text());
                    continue;
                }
            } else {
                console.log(`Collection ${collection.name} already exists.`);
            }
            
            // Define tenant-specific payload field for filtering
            const payloadIndexResponse = await fetch(`${baseUrl}/collections/${collection.name}/index`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    field_name: "tenant_id",
                    field_schema: "keyword"
                })
            });
            
            if (payloadIndexResponse.ok) {
                console.log(`Added tenant_id index to collection: ${collection.name}`);
            } else {
                console.error(`Failed to add tenant_id index to ${collection.name}:`, await payloadIndexResponse.text());
            }
            
        } catch (error) {
            console.error(`Error setting up collection ${collection.name}:`, error);
        }
    }

    // Test vector insertion with tenant namespace
    try {
        console.log('Testing vector insertion with tenant namespace...');
        
        const testVector = Array(TEXT_VECTOR_SIZE).fill(0).map(() => Math.random());
        
        const insertResponse = await fetch(`${baseUrl}/collections/documents/points`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                points: [
                    {
                        id: "test_vector_1",
                        vector: testVector,
                        payload: {
                            tenant_id: "default",
                            title: "Test Document",
                            content: "This is a test document for the default tenant"
                        }
                    }
                ]
            })
        });
        
        if (insertResponse.ok) {
            console.log('Successfully inserted test vector with tenant namespace');
            
            // Test search with tenant filter
            const searchResponse = await fetch(`${baseUrl}/collections/documents/points/search`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    vector: testVector,
                    filter: {
                        must: [
                            {
                                key: "tenant_id",
                                match: { value: "default" }
                            }
                        ]
                    },
                    limit: 1
                })
            });
            
            if (searchResponse.ok) {
                const searchResult = await searchResponse.json();
                console.log('Successfully tested tenant-filtered search:', searchResult);
            } else {
                console.error('Failed tenant-filtered search test:', await searchResponse.text());
            }
        } else {
            console.error('Failed test vector insertion:', await insertResponse.text());
        }
    } catch (error) {
        console.error('Error testing vector operations:', error);
    }

    console.log('Qdrant initialization completed.');
}

// If running in Node.js environment
if (typeof module !== 'undefined' && module.exports) {
    const nodeFetch = require('node-fetch');
    
    // Polyfill fetch for Node.js environments
    if (!globalThis.fetch) {
        globalThis.fetch = nodeFetch;
    }
    
    // Get Qdrant URL from environment or use default
    const qdrantUrl = process.env.QDRANT_URL || 'http://localhost:6333';
    
    // Run the setup
    setupQdrant(qdrantUrl)
        .then(() => console.log('Qdrant setup completed'))
        .catch(err => console.error('Qdrant setup failed:', err));
    
    module.exports = { setupQdrant };
} else {
    // Browser environment - can be run from the console
    console.log('Run setupQdrant() to initialize Qdrant collections');
} 