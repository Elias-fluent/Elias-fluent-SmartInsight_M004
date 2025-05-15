namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Interface for knowledge graph operations
/// </summary>
public interface IKnowledgeGraph
{
    /// <summary>
    /// Adds an entity to the knowledge graph
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <param name="tenantId">ID of the tenant that owns this entity</param>
    /// <returns>The ID of the newly added entity</returns>
    Task<string> AddEntityAsync(IEntity entity, string tenantId);
    
    /// <summary>
    /// Creates a relation between two entities
    /// </summary>
    /// <param name="sourceEntityId">ID of the source entity</param>
    /// <param name="targetEntityId">ID of the target entity</param>
    /// <param name="relationType">Type of relation</param>
    /// <param name="properties">Additional properties for the relation</param>
    /// <param name="tenantId">ID of the tenant that owns this relation</param>
    /// <returns>The ID of the newly created relation</returns>
    Task<string> CreateRelationAsync(
        string sourceEntityId, 
        string targetEntityId, 
        string relationType, 
        IDictionary<string, object>? properties = null,
        string tenantId = "");
    
    /// <summary>
    /// Searches the knowledge graph for entities matching the specified criteria
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="tenantId">ID of the tenant to restrict the search to</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns>Collection of entities matching the criteria</returns>
    Task<IEnumerable<IEntity>> SearchEntitiesAsync(
        string query, 
        string tenantId, 
        int limit = 50);
    
    /// <summary>
    /// Performs a semantic similarity search in the knowledge graph
    /// </summary>
    /// <param name="embedding">The vector embedding to search with</param>
    /// <param name="tenantId">ID of the tenant to restrict the search to</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns>Collection of entities with similarity scores</returns>
    Task<IEnumerable<(IEntity Entity, float Score)>> SimilaritySearchAsync(
        float[] embedding, 
        string tenantId, 
        int limit = 10);
    
    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    /// <param name="entityId">ID of the entity to retrieve</param>
    /// <param name="includeRelations">Whether to include relations in the result</param>
    /// <returns>The entity, or null if not found</returns>
    Task<IEntity?> GetEntityByIdAsync(string entityId, bool includeRelations = false);
} 