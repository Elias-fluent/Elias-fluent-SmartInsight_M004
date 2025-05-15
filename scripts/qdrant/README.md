# Qdrant Vector Database with Tenant Namespace Sharding

This directory contains scripts and configuration for setting up the Qdrant vector database with tenant namespace sharding for SmartInsight.

## Directory Structure

- `config.yaml`: Qdrant configuration with optimized settings for production use
- `init.sh`: Shell script to initialize Qdrant with tenant namespaces
- `init-collections.js`: JavaScript utility to create collections with tenant isolation
- `Dockerfile`: Custom Qdrant image with initialization scripts

## Tenant Namespace Sharding Strategy

SmartInsight uses a tenant-based namespace sharding strategy in Qdrant to enforce tenant isolation:

1. **Collection Design**: Instead of creating separate collections for each tenant, we use a single collection per entity type (documents, entities, search), which is more efficient for storage and management.

2. **Tenant Payload Field**: Each vector stored in Qdrant includes a `tenant_id` field in its payload.

3. **Payload Indexing**: The `tenant_id` field is indexed for fast filtering.

4. **Query Filtering**: Every query to Qdrant includes a filter condition on the `tenant_id` field to ensure tenants can only access their own data.

5. **Performance Optimization**: Using the payload filter approach allows Qdrant to optimize the underlying HNSW index structure across all tenants while still maintaining data isolation.

## Usage in Application

When integrating with Qdrant in your application code, always include the tenant filter in your queries:

```csharp
// Example C# method for searching with tenant isolation
public async Task<List<ScoredPoint>> SearchVectorsAsync(
    string collectionName, 
    float[] queryVector, 
    string tenantId, 
    int limit = 10)
{
    var searchRequest = new SearchRequest
    {
        Vector = queryVector,
        Limit = limit,
        Filter = new Filter
        {
            Must = new List<Condition>
            {
                new Condition
                {
                    Key = "tenant_id",
                    Match = new MatchValue { Value = tenantId }
                }
            }
        }
    };
    
    return await _qdrantClient.SearchAsync(collectionName, searchRequest);
}
```

## Testing Tenant Isolation

The `init.sh` script includes a test function that verifies tenant isolation by:

1. Creating test vectors for different tenants
2. Running search queries with tenant filters
3. Verifying that tenants can only access their own vectors

Run the test manually with:

```bash
cd scripts/qdrant
./init.sh
```

## Docker Integration

The Qdrant container is configured in `docker-compose.yml` to use the custom configuration and initialization scripts.

### Features

- Automatic collection creation with proper indexing
- Tenant isolation through payload filtering
- Optimized HNSW index configuration
- Health checks and monitoring
- Data persistence through Docker volumes

## Best Practices

1. **Always Filter by Tenant**: Every query to Qdrant must include the tenant_id filter.
2. **Index Management**: The tenant_id field is automatically indexed for performance.
3. **Vector Dimensions**: Keep vector dimensions consistent within collections.
4. **Monitoring**: Use Qdrant's metrics endpoint for monitoring collection performance. 