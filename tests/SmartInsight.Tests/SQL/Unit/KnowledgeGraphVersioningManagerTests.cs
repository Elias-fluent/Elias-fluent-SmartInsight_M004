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
    /// Unit tests for the KnowledgeGraphVersioningManager class
    /// </summary>
    public class KnowledgeGraphVersioningManagerTests
    {
        private readonly Mock<ILogger<KnowledgeGraphVersioningManager>> _mockLogger;
        private readonly Mock<ITripleStore> _mockTripleStore;
        private readonly TripleStoreOptions _options;
        private readonly KnowledgeGraphVersioningManager _versioningManager;
        private readonly string _tenantId = "test-tenant";
        
        public KnowledgeGraphVersioningManagerTests()
        {
            _mockLogger = new Mock<ILogger<KnowledgeGraphVersioningManager>>();
            _mockTripleStore = new Mock<ITripleStore>();
            _options = new TripleStoreOptions
            {
                DefaultGraphUri = "http://example.org/graph/default",
                QueryTimeoutSeconds = 30
            };
            
            var optionsWrapper = Options.Create(_options);
            
            _versioningManager = new KnowledgeGraphVersioningManager(
                _mockLogger.Object,
                optionsWrapper,
                _mockTripleStore.Object);
        }
        
        [Fact]
        public async Task RecordVersionAsync_WhenTripleProvided_ShouldCreateVersionRecord()
        {
            // Arrange
            var triple = CreateTestTriple();
            
            // Act
            var version = await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Creation,
                _tenantId,
                "test-user",
                "Initial creation");
                
            // Assert
            Assert.NotNull(version);
            Assert.Equal(triple.Id, version.TripleId);
            Assert.Equal(_tenantId, version.TenantId);
            Assert.Equal(triple.Version, version.VersionNumber);
            Assert.Equal("test-user", version.ChangedByUserId);
            Assert.Equal(ChangeType.Creation, version.ChangeType);
            Assert.Equal("Initial creation", version.ChangeComment);
            Assert.Equal(triple.SubjectId, version.SubjectId);
            Assert.Equal(triple.PredicateUri, version.PredicateUri);
            Assert.Equal(triple.ObjectId, version.ObjectId);
        }
        
        [Fact]
        public async Task GetVersionHistoryAsync_WhenVersionsExist_ShouldReturnVersions()
        {
            // Arrange
            var triple = CreateTestTriple();
            
            await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Creation,
                _tenantId,
                "test-user",
                "Initial creation");
                
            // Update the triple and record a new version
            triple.ObjectId = "UpdatedValue";
            triple.Version = 2;
            
            await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Update,
                _tenantId,
                "test-user",
                "Updated value");
                
            // Act
            var versions = await _versioningManager.GetVersionHistoryAsync(
                triple.Id,
                _tenantId);
                
            // Assert
            Assert.NotNull(versions);
            Assert.Equal(2, versions.Count);
            Assert.Equal(2, versions[0].VersionNumber); // Most recent first
            Assert.Equal(1, versions[1].VersionNumber);
            Assert.Equal("UpdatedValue", versions[0].ObjectId);
            Assert.Equal("TestValue", versions[1].ObjectId);
        }
        
        [Fact]
        public async Task GetVersionAsync_WhenVersionExists_ShouldReturnVersion()
        {
            // Arrange
            var triple = CreateTestTriple();
            
            await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Creation,
                _tenantId,
                "test-user",
                "Initial creation");
                
            // Act
            var version = await _versioningManager.GetVersionAsync(
                triple.Id,
                1,
                _tenantId);
                
            // Assert
            Assert.NotNull(version);
            Assert.Equal(triple.Id, version.TripleId);
            Assert.Equal(1, version.VersionNumber);
            Assert.Equal(ChangeType.Creation, version.ChangeType);
        }
        
        [Fact]
        public async Task GetVersionDiffAsync_WhenVersionsExist_ShouldReturnDiff()
        {
            // Arrange
            var triple = CreateTestTriple();
            
            await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Creation,
                _tenantId,
                "test-user",
                "Initial creation");
                
            // Update the triple and record a new version
            triple.ObjectId = "UpdatedValue";
            triple.Version = 2;
            
            await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Update,
                _tenantId,
                "test-user",
                "Updated value");
                
            // Act
            var diff = await _versioningManager.GetVersionDiffAsync(
                triple.Id,
                1,
                2,
                _tenantId);
                
            // Assert
            Assert.NotNull(diff);
            Assert.Equal(triple.Id, diff.TripleId);
            Assert.Equal(1, diff.FromVersion);
            Assert.Equal(2, diff.ToVersion);
            Assert.False(diff.SubjectChange.HasChanged);
            Assert.False(diff.PredicateChange.HasChanged);
            Assert.True(diff.ObjectChange.HasChanged);
            Assert.Equal("TestValue", diff.ObjectChange.OldValue);
            Assert.Equal("UpdatedValue", diff.ObjectChange.NewValue);
        }
        
        [Fact]
        public async Task QueryTemporalAsync_WithAsOfDate_ShouldReturnStateAtPointInTime()
        {
            // Arrange
            var triple = CreateTestTriple();
            var now = DateTime.UtcNow;
            var oneHourAgo = now.AddHours(-1);
            
            // Mock the behavior of the triple store
            _mockTripleStore
                .Setup(x => x.QueryAsync(It.IsAny<TripleQuery>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TripleQueryResult
                {
                    Triples = new List<Triple> { triple }
                });
                
            // Create initial version with timestamp one hour ago
            var initialVersion = await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Creation,
                _tenantId,
                "test-user",
                "Initial creation");
                
            // Access the private field to modify the timestamp (for testing)
            var versionField = initialVersion.GetType().GetProperty("CreatedAt");
            versionField.SetValue(initialVersion, oneHourAgo);
            
            // Update the triple
            triple.ObjectId = "UpdatedValue";
            triple.Version = 2;
            
            // Create update version with current timestamp
            await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Update,
                _tenantId,
                "test-user",
                "Updated value");
                
            // Act - Query as of 30 minutes ago (should get version 1)
            var query = TemporalQuery.AsOf(now.AddMinutes(-30));
            var result = await _versioningManager.QueryTemporalAsync(query, _tenantId);
                
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Triples.Count > 0);
            
            // The first triple should have the original value
            var tripleAtPoint = result.Triples.FirstOrDefault();
            Assert.NotNull(tripleAtPoint);
            Assert.Equal("TestValue", tripleAtPoint.ObjectId);
            Assert.Equal(1, tripleAtPoint.Version);
        }
        
        [Fact]
        public async Task CreateSnapshotAsync_WhenCalled_ShouldCreateSnapshot()
        {
            // Arrange
            var triple = CreateTestTriple();
            
            // Mock the behavior of the triple store
            _mockTripleStore
                .Setup(x => x.QueryAsync(It.IsAny<TripleQuery>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TripleQueryResult
                {
                    Triples = new List<Triple> { triple }
                });
                
            // Act
            var result = await _versioningManager.CreateSnapshotAsync(
                "test-snapshot",
                _tenantId);
                
            // Assert
            Assert.True(result);
            
            // Verify that we can retrieve the snapshot metadata
            var snapshots = await _versioningManager.GetAvailableSnapshotsAsync(_tenantId);
            Assert.NotNull(snapshots);
            Assert.True(snapshots.ContainsKey("test-snapshot"));
            
            var snapshotMeta = snapshots["test-snapshot"];
            Assert.Equal("test-snapshot", snapshotMeta["Name"]);
            Assert.Equal(_tenantId, snapshotMeta["TenantId"]);
            Assert.Equal(1, snapshotMeta["TripleCount"]);
        }
        
        [Fact]
        public async Task RestoreSnapshotAsync_WhenSnapshotExists_ShouldRestoreTriples()
        {
            // Arrange
            var triple = CreateTestTriple();
            
            // Mock the behavior of the triple store for queries
            _mockTripleStore
                .Setup(x => x.QueryAsync(It.IsAny<TripleQuery>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TripleQueryResult
                {
                    Triples = new List<Triple> { triple }
                });

            // Mock the RemoveGraphAsync method
            _mockTripleStore
                .Setup(x => x.RemoveGraphAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            // Mock the AddTripleAsync method
            _mockTripleStore
                .Setup(x => x.AddTripleAsync(It.IsAny<Triple>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            // Create a snapshot
            await _versioningManager.CreateSnapshotAsync("test-snapshot", _tenantId);
            
            // Act
            var result = await _versioningManager.RestoreSnapshotAsync("test-snapshot", _tenantId);
            
            // Assert
            Assert.True(result);
            
            // Verify that RemoveGraphAsync was called
            _mockTripleStore.Verify(
                x => x.RemoveGraphAsync(
                    It.Is<string>(g => g == _options.DefaultGraphUri),
                    It.Is<string>(t => t == _tenantId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
                
            // Verify that AddTripleAsync was called for each triple in the snapshot
            _mockTripleStore.Verify(
                x => x.AddTripleAsync(
                    It.IsAny<Triple>(),
                    It.Is<string>(t => t == _tenantId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Fact]
        public async Task QueryTemporalAsync_WithTimeRange_ShouldReturnTripleVersions()
        {
            // Arrange
            var triple = CreateTestTriple();
            var now = DateTime.UtcNow;
            var twoHoursAgo = now.AddHours(-2);
            var oneHourAgo = now.AddHours(-1);
            
            // Mock the behavior of the triple store
            _mockTripleStore
                .Setup(x => x.QueryAsync(It.IsAny<TripleQuery>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TripleQueryResult
                {
                    Triples = new List<Triple> { triple }
                });
                
            // Create initial version with timestamp two hours ago
            var initialVersion = await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Creation,
                _tenantId,
                "test-user",
                "Initial creation");
                
            // Access the private field to modify the timestamp (for testing)
            var versionField = initialVersion.GetType().GetProperty("CreatedAt");
            versionField.SetValue(initialVersion, twoHoursAgo);
            
            // Update the triple
            triple.ObjectId = "UpdatedValue";
            triple.Version = 2;
            
            // Create update version with timestamp one hour ago
            var updateVersion = await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Update,
                _tenantId,
                "test-user",
                "Updated value");
                
            versionField.SetValue(updateVersion, oneHourAgo);
            
            // Act - Query from three hours ago to 30 minutes ago
            var query = new TemporalQuery
            {
                FromDate = now.AddHours(-3),
                ToDate = now.AddMinutes(-30),
                IncludeAllVersions = true
            };
            
            var result = await _versioningManager.QueryTemporalAsync(query, _tenantId);
                
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TripleVersions.Count);
            Assert.Equal(1, result.TripleVersions.Count(v => v.ChangeType == ChangeType.Creation));
            Assert.Equal(1, result.TripleVersions.Count(v => v.ChangeType == ChangeType.Update));
        }
        
        [Fact]
        public async Task QueryTemporalAsync_WithVersionNumber_ShouldReturnSpecificVersions()
        {
            // Arrange
            var triple = CreateTestTriple();
            
            // Mock the behavior of the triple store
            _mockTripleStore
                .Setup(x => x.QueryAsync(It.IsAny<TripleQuery>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TripleQueryResult
                {
                    Triples = new List<Triple> { triple }
                });
                
            // Create initial version
            await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Creation,
                _tenantId,
                "test-user",
                "Initial creation");
                
            // Update the triple
            triple.ObjectId = "UpdatedValue";
            triple.Version = 2;
            
            // Create update version
            await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Update,
                _tenantId,
                "test-user",
                "Updated value");
                
            // Act - Query for version 1
            var query = new TemporalQuery
            {
                VersionNumber = 1
            };
            
            var result = await _versioningManager.QueryTemporalAsync(query, _tenantId);
                
            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TripleVersions.Count);
            Assert.Equal(1, result.TripleVersions[0].VersionNumber);
            Assert.Equal(ChangeType.Creation, result.TripleVersions[0].ChangeType);
            Assert.Equal("TestValue", result.TripleVersions[0].ObjectId);
        }
        
        [Fact]
        public async Task RestoreVersionAsync_WhenVersionExists_ShouldRestoreTriple()
        {
            // Arrange
            var triple = CreateTestTriple();
            
            // Create initial version
            await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Creation,
                _tenantId,
                "test-user",
                "Initial creation");
                
            // Update the triple
            triple.ObjectId = "UpdatedValue";
            triple.Version = 2;
            
            // Create update version
            await _versioningManager.RecordVersionAsync(
                triple,
                ChangeType.Update,
                _tenantId,
                "test-user",
                "Updated value");
                
            // Mock the UpdateTripleAsync method
            _mockTripleStore
                .Setup(x => x.UpdateTripleAsync(It.IsAny<Triple>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
                
            // Act - Restore to version 1
            var restoredTriple = await _versioningManager.RestoreVersionAsync(
                triple.Id,
                1,
                _tenantId,
                "test-user",
                "Restoring to original version");
                
            // Assert
            Assert.NotNull(restoredTriple);
            Assert.Equal(triple.Id, restoredTriple.Id);
            Assert.Equal(_tenantId, restoredTriple.TenantId);
            Assert.Equal("TestValue", restoredTriple.ObjectId);
            Assert.Equal(3, restoredTriple.Version); // Should be incremented to version 3
            
            // Verify that UpdateTripleAsync was called
            _mockTripleStore.Verify(
                x => x.UpdateTripleAsync(
                    It.Is<Triple>(t => t.ObjectId == "TestValue" && t.Id == triple.Id),
                    It.Is<string>(t => t == _tenantId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Fact]
        public async Task GetAvailableSnapshotsAsync_WhenSnapshotsExist_ShouldReturnMetadata()
        {
            // Arrange
            var triple1 = CreateTestTriple();
            var triple2 = CreateTestTriple();
            
            // Mock the behavior of the triple store
            _mockTripleStore
                .Setup(x => x.QueryAsync(It.IsAny<TripleQuery>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TripleQueryResult
                {
                    Triples = new List<Triple> { triple1, triple2 }
                });
                
            // Create two snapshots
            await _versioningManager.CreateSnapshotAsync("snapshot1", _tenantId);
            await _versioningManager.CreateSnapshotAsync("snapshot2", _tenantId);
            
            // Act
            var snapshots = await _versioningManager.GetAvailableSnapshotsAsync(_tenantId);
            
            // Assert
            Assert.NotNull(snapshots);
            Assert.Equal(2, snapshots.Count);
            Assert.Contains("snapshot1", snapshots.Keys);
            Assert.Contains("snapshot2", snapshots.Keys);
            
            // Verify metadata for snapshots
            Assert.Equal("snapshot1", snapshots["snapshot1"]["Name"]);
            Assert.Equal(_tenantId, snapshots["snapshot1"]["TenantId"]);
            Assert.Equal(2, snapshots["snapshot1"]["TripleCount"]);
            
            Assert.Equal("snapshot2", snapshots["snapshot2"]["Name"]);
            Assert.Equal(_tenantId, snapshots["snapshot2"]["TenantId"]);
            Assert.Equal(2, snapshots["snapshot2"]["TripleCount"]);
        }
        
        private Triple CreateTestTriple()
        {
            return new Triple
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = _tenantId,
                SubjectId = "TestSubject",
                PredicateUri = "http://example.org/predicate/hasValue",
                ObjectId = "TestValue",
                IsLiteral = true,
                GraphUri = _options.DefaultGraphUri,
                ConfidenceScore = 0.95,
                Version = 1
            };
        }
    }
} 