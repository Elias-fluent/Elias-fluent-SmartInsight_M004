using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping;
using SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;
using Xunit;

namespace SmartInsight.Tests.SQL.Unit
{
    public class RelationToTripleMapperTests
    {
        private readonly Mock<ILogger<RelationToTripleMapper>> _mockLogger;
        private readonly Mock<ITripleStore> _mockTripleStore;
        private readonly RelationToTripleMapper _mapper;
        private readonly string _tenantId = "test-tenant";
        
        public RelationToTripleMapperTests()
        {
            _mockLogger = new Mock<ILogger<RelationToTripleMapper>>();
            _mockTripleStore = new Mock<ITripleStore>();
            _mapper = new RelationToTripleMapper(_mockLogger.Object, _mockTripleStore.Object);
        }
        
        [Fact]
        public async Task MapAndStoreAsync_ValidRelation_CallsTripleStore()
        {
            // Arrange
            var relation = CreateTestRelation();
            _mockTripleStore
                .Setup(ts => ts.AddTripleAsync(It.IsAny<Triple>(), _tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            // Act
            var result = await _mapper.MapAndStoreAsync(relation, _tenantId);
            
            // Assert
            Assert.True(result);
            _mockTripleStore.Verify(
                ts => ts.AddTripleAsync(It.IsAny<Triple>(), _tenantId, It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Fact]
        public async Task MapAndStoreBatchAsync_ValidRelations_CallsTripleStore()
        {
            // Arrange
            var relations = new List<Relation>
            {
                CreateTestRelation(),
                CreateTestRelation()
            };
            
            _mockTripleStore
                .Setup(ts => ts.AddTriplesAsync(It.IsAny<IEnumerable<Triple>>(), _tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);
                
            // Act
            var result = await _mapper.MapAndStoreBatchAsync(relations, _tenantId);
            
            // Assert
            Assert.Equal(2, result);
            _mockTripleStore.Verify(
                ts => ts.AddTriplesAsync(It.IsAny<IEnumerable<Triple>>(), _tenantId, It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Fact]
        public async Task MapAndStoreBatchAsync_BidirectionalRelation_CreatesInverseTriple()
        {
            // Arrange
            var relation = CreateTestRelation();
            relation.IsDirectional = false;
            
            var relations = new List<Relation> { relation };
            
            // Setup _mockTripleStore to capture the triples passed to it
            List<Triple> capturedTriples = null;
            _mockTripleStore
                .Setup(ts => ts.AddTriplesAsync(It.IsAny<IEnumerable<Triple>>(), _tenantId, It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<Triple>, string, CancellationToken>((triples, _, __) => capturedTriples = new List<Triple>(triples))
                .ReturnsAsync(2);
                
            // Act
            var result = await _mapper.MapAndStoreBatchAsync(relations, _tenantId);
            
            // Assert
            Assert.Equal(2, result); // Should add both original and inverse triples
            Assert.NotNull(capturedTriples);
            Assert.Equal(2, capturedTriples.Count); // Should have 2 triples (original + inverse)
            
            // Verify inverse triple properties
            var originalTriple = capturedTriples[0];
            var inverseTriple = capturedTriples[1];
            
            Assert.Equal($"{relation.Id}_inverse", inverseTriple.Id);
            Assert.Equal(originalTriple.ObjectId, inverseTriple.SubjectId); // Swapped subject/object
            Assert.Equal(originalTriple.SubjectId, inverseTriple.ObjectId); // Swapped subject/object
        }
        
        [Fact]
        public async Task MapAndStoreAsync_DomainSpecificRelation_SetsCustomPredicateUri()
        {
            // Arrange
            var relation = CreateTestRelation();
            relation.RelationType = RelationType.DomainSpecific;
            relation.RelationName = "customRelation";
            
            Triple capturedTriple = null;
            _mockTripleStore
                .Setup(ts => ts.AddTripleAsync(It.IsAny<Triple>(), _tenantId, It.IsAny<CancellationToken>()))
                .Callback<Triple, string, CancellationToken>((triple, _, __) => capturedTriple = triple)
                .ReturnsAsync(true);
                
            // Act
            var result = await _mapper.MapAndStoreAsync(relation, _tenantId);
            
            // Assert
            Assert.True(result);
            Assert.NotNull(capturedTriple);
            Assert.Equal($"http://smartinsight.com/ontology/domain/{Uri.EscapeDataString("customRelation")}", capturedTriple.PredicateUri);
        }
        
        [Fact]
        public async Task MapAndStoreAsync_WithCustomGraphUri_UsesSpecifiedUri()
        {
            // Arrange
            var relation = CreateTestRelation();
            var customGraphUri = "http://smartinsight.com/graph/custom";
            
            Triple capturedTriple = null;
            _mockTripleStore
                .Setup(ts => ts.AddTripleAsync(It.IsAny<Triple>(), _tenantId, It.IsAny<CancellationToken>()))
                .Callback<Triple, string, CancellationToken>((triple, _, __) => capturedTriple = triple)
                .ReturnsAsync(true);
                
            // Act
            var result = await _mapper.MapAndStoreAsync(relation, _tenantId, customGraphUri);
            
            // Assert
            Assert.True(result);
            Assert.NotNull(capturedTriple);
            Assert.Equal(customGraphUri, capturedTriple.GraphUri);
        }
        
        private Relation CreateTestRelation()
        {
            var sourceEntity = new Entity 
            { 
                Id = Guid.NewGuid().ToString(),
                Name = "Person A",
                Type = EntityType.Person 
            };
            
            var targetEntity = new Entity 
            { 
                Id = Guid.NewGuid().ToString(),
                Name = "Organization B",
                Type = EntityType.Organization 
            };
            
            return new Relation
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = _tenantId,
                SourceEntity = sourceEntity,
                SourceEntityId = sourceEntity.Id,
                TargetEntity = targetEntity,
                TargetEntityId = targetEntity.Id,
                RelationType = RelationType.WorksFor,
                ConfidenceScore = 0.9,
                IsDirectional = true,
                SourceContext = "Person A works for Organization B",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExtractionMethod = "UnitTest"
            };
        }
    }
} 