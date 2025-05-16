using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Interfaces;
using SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models;
using Xunit;

namespace SmartInsight.Tests.SQL.Unit
{
    /// <summary>
    /// Unit tests for the temporal query functionality
    /// </summary>
    public class TemporalQueryTests
    {
        private readonly Mock<ILogger<KnowledgeGraphVersioningManager>> _mockLogger;
        private readonly Mock<ITripleStore> _mockTripleStore;
        private readonly IOptions<TripleStoreOptions> _options;
        private readonly KnowledgeGraphVersioningManager _versioningManager;
        private readonly string _tenantId = "test-tenant";
        
        /// <summary>
        /// Sets up the test environment
        /// </summary>
        public TemporalQueryTests()
        {
            _mockLogger = new Mock<ILogger<KnowledgeGraphVersioningManager>>();
            _mockTripleStore = new Mock<ITripleStore>();
            
            // Set up options
            _options = Options.Create(new TripleStoreOptions
            {
                DefaultGraphUri = "http://test.com/graph",
                QueryTimeoutSeconds = 30
            });
            
            // Create the versioning manager
            _versioningManager = new KnowledgeGraphVersioningManager(
                _mockLogger.Object,
                _options,
                _mockTripleStore.Object);
                
            // Mock the AddTripleAsync method
            _mockTripleStore
                .Setup(x => x.AddTripleAsync(It.IsAny<Triple>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            // Mock the RemoveTripleAsync method
            _mockTripleStore
                .Setup(x => x.RemoveGraphAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            // Mock the UpdateTripleAsync method
            _mockTripleStore
                .Setup(x => x.UpdateTripleAsync(It.IsAny<Triple>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }
        
        /// <summary>
        /// Tests querying as of a specific date
        /// </summary>
        [Fact]
        public async Task TemporalQuery_AsOfDate_ReturnsCorrectTriples()
        {
            // Arrange
            var triple1 = new Triple
            {
                Id = "triple1",
                SubjectId = "subject1",
                PredicateUri = "predicate1",
                ObjectId = "object1",
                TenantId = _tenantId,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };
            
            var triple2 = new Triple
            {
                Id = "triple2",
                SubjectId = "subject2",
                PredicateUri = "predicate2",
                ObjectId = "object2",
                TenantId = _tenantId,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            
            // Record versions for the test
            await _versioningManager.RecordVersionAsync(triple1, ChangeType.Creation, _tenantId);
            await _versioningManager.RecordVersionAsync(triple2, ChangeType.Creation, _tenantId);
            
            // Update triple1 after creation
            triple1.ObjectId = "newObject1";
            await _versioningManager.RecordVersionAsync(triple1, ChangeType.Update, _tenantId);
            
            // Create temporal query for a date between the creation and update
            var queryDate = DateTime.UtcNow.AddDays(-7);
            var query = TemporalQuery.AsOf(queryDate);
            
            // Act
            var result = await _versioningManager.QueryTemporalAsync(query, _tenantId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Triples.Count);
            Assert.Equal("triple1", result.Triples[0].Id);
            Assert.Equal("object1", result.Triples[0].ObjectId); // Original value before update
            Assert.Equal(queryDate, result.Query.AsOfDate);
        }
        
        /// <summary>
        /// Tests querying between two dates
        /// </summary>
        [Fact]
        public async Task TemporalQuery_BetweenDates_ReturnsChangesInRange()
        {
            // Arrange
            var triple = new Triple
            {
                Id = "triple3",
                SubjectId = "subject3",
                PredicateUri = "predicate3",
                ObjectId = "object3",
                TenantId = _tenantId,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            };
            
            // Record initial version
            await _versioningManager.RecordVersionAsync(triple, ChangeType.Creation, _tenantId, null, "Initial creation");
            
            // Wait a bit to ensure timestamps are different
            await Task.Delay(100);
            
            // First update outside range
            triple.ObjectId = "update1";
            await _versioningManager.RecordVersionAsync(triple, ChangeType.Update, _tenantId, null, "First update");
            
            // Second update in range
            var fromDate = DateTime.UtcNow.AddDays(-12);
            
            // Wait a bit to ensure timestamps are different
            await Task.Delay(100);
            
            triple.ObjectId = "update2";
            await _versioningManager.RecordVersionAsync(triple, ChangeType.Update, _tenantId, null, "Second update");
            
            // Third update in range
            await Task.Delay(100);
            
            triple.ObjectId = "update3";
            await _versioningManager.RecordVersionAsync(triple, ChangeType.Update, _tenantId, null, "Third update");
            
            // Delete in range
            var toDate = DateTime.UtcNow.AddDays(-8);
            
            // Create query for changes between dates
            var query = TemporalQuery.BetweenDates(fromDate, toDate);
            
            // Act
            var result = await _versioningManager.QueryTemporalAsync(query, _tenantId);
            
            // Assert
            Assert.NotNull(result);
            Assert.True(result.TripleVersions.Count >= 2); // Should have at least the 2 updates in range
            Assert.Equal(fromDate, result.Query.FromDate);
            Assert.Equal(toDate, result.Query.ToDate);
            
            // Verify all versions in the range have the right timestamps
            foreach (var version in result.TripleVersions)
            {
                Assert.True(version.CreatedAt >= fromDate);
                Assert.True(version.CreatedAt <= toDate);
            }
        }
        
        /// <summary>
        /// Tests filtering by change type
        /// </summary>
        [Fact]
        public async Task TemporalQuery_FilterByChangeType_ReturnsOnlySpecifiedChanges()
        {
            // Arrange
            var triple = new Triple
            {
                Id = "triple4",
                SubjectId = "subject4",
                PredicateUri = "predicate4",
                ObjectId = "object4",
                TenantId = _tenantId
            };
            
            // Record creation
            await _versioningManager.RecordVersionAsync(triple, ChangeType.Creation, _tenantId);
            
            // Record update
            triple.ObjectId = "updatedObject4";
            await _versioningManager.RecordVersionAsync(triple, ChangeType.Update, _tenantId);
            
            // Record deletion
            await _versioningManager.RecordVersionAsync(triple, ChangeType.Deletion, _tenantId);
            
            // Create query for only updates
            var query = new TemporalQuery
            {
                IncludeAllVersions = true,
                ChangeTypes = new[] { ChangeType.Update }
            };
            
            // Act
            var result = await _versioningManager.QueryTemporalAsync(query, _tenantId);
            
            // Assert
            Assert.NotNull(result);
            Assert.True(result.TripleVersions.Count > 0);
            
            // Verify all returned versions are of type Update
            foreach (var version in result.TripleVersions)
            {
                Assert.Equal(ChangeType.Update, version.ChangeType);
            }
        }
    }
} 