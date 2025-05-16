# Relation Mapping Pipeline

## Overview

The Relation Mapping Pipeline is a component of the SmartInsight Knowledge Graph Creation Pipeline that extracts relationships between entities and converts them to triples for storage in the triple store database.

## Architecture

The relation mapping pipeline consists of the following key components:

1. **Relation Extractors**: These identify specific types of relationships between entities based on text content and entity types.
2. **Relation Extractor Factory**: Creates relation extractors and manages their lifecycle.
3. **Relation Mapping Pipeline**: Coordinates the extraction process, deduplicates and validates relations.
4. **Relation to Triple Mapper**: Converts relations to RDF triples for storage in the triple store.
5. **Triple Store**: Provides storage and query capabilities for the generated triples.

## Key Interfaces

### IRelationMappingPipeline

Coordinates the extraction of relations from text content and entities.

```csharp
public interface IRelationMappingPipeline
{
    Task<IEnumerable<Relation>> ProcessAsync(
        string content,
        IEnumerable<Entity> entities,
        string sourceDocumentId,
        string tenantId,
        IEnumerable<string> extractorTypes = null,
        CancellationToken cancellationToken = default);
        
    IEnumerable<IRelationExtractor> GetRegisteredExtractors();
    
    IRelationExtractor GetExtractor(string typeName);
    
    IEnumerable<RelationType> GetSupportedRelationTypes();
    
    IEnumerable<Relation> ValidateRelations(
        IEnumerable<Relation> relations,
        double minConfidenceThreshold = 0.5);
}
```

### IRelationExtractor

Extracts specific types of relations between entities.

```csharp
public interface IRelationExtractor
{
    Task<IEnumerable<Relation>> ExtractRelationsAsync(
        string content,
        IEnumerable<Entity> entities,
        string sourceDocumentId,
        string tenantId,
        CancellationToken cancellationToken = default);
        
    IEnumerable<RelationType> GetSupportedRelationTypes();
    
    IEnumerable<RelationExtractionPattern> GetExtractionPatterns();
    
    bool ValidateRelation(
        Entity sourceEntity,
        Entity targetEntity,
        RelationType relationType);
}
```

### IRelationToTripleMapper

Maps relations to triples for storage in the triple store.

```csharp
public interface IRelationToTripleMapper
{
    Task<bool> MapAndStoreAsync(
        Relation relation, 
        string tenantId, 
        string graphUri = null, 
        CancellationToken cancellationToken = default);
        
    Task<int> MapAndStoreBatchAsync(
        IEnumerable<Relation> relations, 
        string tenantId, 
        string graphUri = null, 
        CancellationToken cancellationToken = default);
}
```

## Implementations

### RelationMappingPipeline

The default implementation of the relation mapping pipeline.

### AdvancedRelationMappingPipeline

An enhanced implementation with additional features:

- Automatic conversion of relations to triples
- Additional validation rules for relations
- Support for self-relations filtering
- Improved entity type validation

### RelationToTripleMapper

Converts relations to triples based on their relation type, mapping to appropriate predicate URIs in the triple store.

## Models

### Relation

Represents a relationship between two entities.

```csharp
public class Relation
{
    public string Id { get; set; }
    public string TenantId { get; set; }
    public Entity SourceEntity { get; set; }
    public string SourceEntityId { get; set; }
    public Entity TargetEntity { get; set; }
    public string TargetEntityId { get; set; }
    public RelationType RelationType { get; set; }
    public string RelationName { get; set; }
    public double ConfidenceScore { get; set; }
    public string SourceContext { get; set; }
    public string SourceDocumentId { get; set; }
    public bool IsDirectional { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string ExtractionMethod { get; set; }
    public bool IsVerified { get; set; }
    public int Version { get; set; }
    public Dictionary<string, object> Attributes { get; set; }
}
```

### RelationType

Enumeration of supported relation types.

```csharp
public enum RelationType
{
    AssociatedWith,
    WorksFor,
    LocatedIn,
    HeadquarteredIn,
    HasTitle,
    HasSkill,
    Created,
    PartOf,
    Owns,
    SubsidiaryOf,
    AuthorOf,
    Leads,
    ParticipatesIn,
    OccurredBefore,
    OccurredAfter,
    DomainSpecific,
    Uses,
    DependsOn,
    SimilarTo,
    References,
    SynonymOf,
    ParentCategoryOf,
    SubcategoryOf,
    ColumnOf,
    TableOf,
    HasAttribute,
    Other
}
```

### RelationMappingOptions

Configuration options for the relation mapping pipeline.

```csharp
public class RelationMappingOptions
{
    public double MinConfidenceThreshold { get; set; } = 0.5;
    public bool AllowSelfRelations { get; set; } = false;
    public bool ValidateEntityTypes { get; set; } = true;
    public bool AutoConvertToTriples { get; set; } = true;
    public string DefaultGraphUri { get; set; } = null;
}
```

## Integration with Triple Store

The relation mapping pipeline integrates with the triple store via the `ITripleStore` interface. Relations are converted to triples using the `RelationToTripleMapper` component, which maps relation types to predicate URIs.

Predicate URIs follow the pattern: `http://smartinsight.com/ontology/{relationType}`

For domain-specific relations, the pattern is: `http://smartinsight.com/ontology/domain/{relationName}`

## Multi-Tenant Support

All components in the relation mapping pipeline support multi-tenant isolation through tenant IDs. Each relation and triple is associated with a specific tenant, ensuring data isolation between tenants.

## Registration

### Default Registration

```csharp
services.AddRelationMapping();
```

### With Configuration

```csharp
services.AddRelationMapping(configuration.GetSection("RelationMapping"));
```

### With Options

```csharp
services.AddRelationMapping(options =>
{
    options.MinConfidenceThreshold = 0.7;
    options.AllowSelfRelations = true;
    options.ValidateEntityTypes = true;
    options.AutoConvertToTriples = true;
});
```

## Example Usage

```csharp
public class RelationMappingDemo
{
    private readonly IRelationMappingPipeline _pipeline;
    private readonly IRelationToTripleMapper _mapper;
    
    public RelationMappingDemo(
        IRelationMappingPipeline pipeline,
        IRelationToTripleMapper mapper)
    {
        _pipeline = pipeline;
        _mapper = mapper;
    }
    
    public async Task ProcessDocument(string content, string tenantId)
    {
        // Extract entities (using separate component)
        var entities = await _entityExtractionPipeline.ExtractEntitiesAsync(content, tenantId);
        
        // Extract relations
        var relations = await _pipeline.ProcessAsync(content, entities, "document-123", tenantId);
        
        Console.WriteLine($"Extracted {relations.Count()} relations");
        
        // Map and store relations as triples (if not done automatically)
        if (relations.Any())
        {
            int count = await _mapper.MapAndStoreBatchAsync(relations, tenantId);
            Console.WriteLine($"Stored {count} triples in the triple store");
        }
    }
}
```

## Extending with Custom Relation Extractors

You can create custom relation extractors by implementing the `IRelationExtractor` interface:

```csharp
public class CustomRelationExtractor : IRelationExtractor
{
    public async Task<IEnumerable<Relation>> ExtractRelationsAsync(
        string content,
        IEnumerable<Entity> entities,
        string sourceDocumentId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        // Custom extraction logic
        // ...
        
        return relations;
    }
    
    public IEnumerable<RelationType> GetSupportedRelationTypes()
    {
        return new[] { RelationType.CustomType };
    }
    
    // Implement other interface methods
}
```

Register your custom extractor:

```csharp
services.AddSingleton<IRelationExtractor, CustomRelationExtractor>();
``` 