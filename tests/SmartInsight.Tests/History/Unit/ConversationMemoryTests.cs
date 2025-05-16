using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartInsight.Core.Entities;
using SmartInsight.Core.Interfaces;
using SmartInsight.Data.Contexts;
using SmartInsight.History;
using SmartInsight.History.Interfaces;
using SmartInsight.History.Models;
using Xunit;

namespace SmartInsight.Tests.History.Unit;

public class ConversationMemoryTests
{
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly Mock<ITenantAccessor> _mockTenantAccessor;
    private readonly Mock<ILogger<ConversationMemory>> _mockLogger;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    
    public ConversationMemoryTests()
    {
        // Set up in-memory database for testing
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"ConversationMemoryTests_{Guid.NewGuid()}")
            .Options;
            
        _mockTenantAccessor = new Mock<ITenantAccessor>();
        _mockTenantAccessor.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);
        
        _mockLogger = new Mock<ILogger<ConversationMemory>>();
    }
    
    [Fact]
    public async Task CreateSessionAsync_ShouldReturnNewSessionId()
    {
        // Arrange
        using var dbContext = new ApplicationDbContext(_dbContextOptions);
        var conversationMemory = new ConversationMemory(dbContext, _mockTenantAccessor.Object, _mockLogger.Object);
        
        // Act
        var sessionId = await conversationMemory.CreateSessionAsync(_userId);
        
        // Assert
        Assert.NotEqual(Guid.Empty, sessionId);
    }
    
    [Fact]
    public async Task AddUserMessageAsync_ShouldCreateNewConversationLog()
    {
        // Arrange
        using var dbContext = new ApplicationDbContext(_dbContextOptions);
        var conversationMemory = new ConversationMemory(dbContext, _mockTenantAccessor.Object, _mockLogger.Object);
        var sessionId = Guid.NewGuid();
        var query = "Test query";
        
        // Act
        var result = await conversationMemory.AddUserMessageAsync(sessionId, _userId, query);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal(_tenantId, result.TenantId);
        Assert.Equal(query, result.Query);
        Assert.Equal(1, result.SequenceNumber);
        
        // Verify it was saved to the database
        var savedLog = await dbContext.ConversationLogs.FirstOrDefaultAsync(c => c.SessionId == sessionId);
        Assert.NotNull(savedLog);
        Assert.Equal(query, savedLog!.Query);
    }
    
    [Fact]
    public async Task AddSystemResponseAsync_ShouldUpdateExistingConversationLog()
    {
        // Arrange
        using var dbContext = new ApplicationDbContext(_dbContextOptions);
        var conversationMemory = new ConversationMemory(dbContext, _mockTenantAccessor.Object, _mockLogger.Object);
        var sessionId = Guid.NewGuid();
        
        // Create a conversation log first
        var conversationLog = new ConversationLog
        {
            SessionId = sessionId,
            UserId = _userId,
            TenantId = _tenantId,
            Query = "Test query",
            QueryTimestamp = DateTime.UtcNow,
            SequenceNumber = 1
        };
        
        dbContext.ConversationLogs.Add(conversationLog);
        await dbContext.SaveChangesAsync();
        
        var response = "Test response";
        
        // Act
        var result = await conversationMemory.AddSystemResponseAsync(
            conversationLog.Id, 
            response, 
            true, 
            null, 
            "SELECT * FROM test", 
            "Knowledge Base", 
            "GPT-4", 
            150, 
            500);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(response, result.Response);
        Assert.Equal("SELECT * FROM test", result.GeneratedSql);
        Assert.Equal("Knowledge Base", result.KnowledgeSource);
        Assert.Equal("GPT-4", result.ModelUsed);
        Assert.Equal(150, result.TokensUsed);
        Assert.Equal(500, result.ProcessingTimeMs);
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.ResponseTimestamp);
        
        // Verify it was updated in the database
        var updatedLog = await dbContext.ConversationLogs.FindAsync(conversationLog.Id);
        Assert.NotNull(updatedLog);
        Assert.Equal(response, updatedLog!.Response);
    }
    
    [Fact]
    public async Task GetSessionHistoryAsync_ShouldReturnOrderedConversationLogs()
    {
        // Arrange
        using var dbContext = new ApplicationDbContext(_dbContextOptions);
        var conversationMemory = new ConversationMemory(dbContext, _mockTenantAccessor.Object, _mockLogger.Object);
        var sessionId = Guid.NewGuid();
        
        // Create multiple conversation logs
        var logs = new List<ConversationLog>
        {
            new ConversationLog
            {
                SessionId = sessionId,
                UserId = _userId,
                TenantId = _tenantId,
                Query = "First query",
                QueryTimestamp = DateTime.UtcNow.AddMinutes(-10),
                SequenceNumber = 1
            },
            new ConversationLog
            {
                SessionId = sessionId,
                UserId = _userId,
                TenantId = _tenantId,
                Query = "Second query",
                QueryTimestamp = DateTime.UtcNow.AddMinutes(-5),
                SequenceNumber = 2
            },
            new ConversationLog
            {
                SessionId = sessionId,
                UserId = _userId,
                TenantId = _tenantId,
                Query = "Third query",
                QueryTimestamp = DateTime.UtcNow,
                SequenceNumber = 3
            }
        };
        
        dbContext.ConversationLogs.AddRange(logs);
        await dbContext.SaveChangesAsync();
        
        // Act
        var result = await conversationMemory.GetSessionHistoryAsync(sessionId);
        
        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].SequenceNumber);
        Assert.Equal(2, result[1].SequenceNumber);
        Assert.Equal(3, result[2].SequenceNumber);
        Assert.Equal("First query", result[0].Query);
        Assert.Equal("Third query", result[2].Query);
    }
    
    [Fact]
    public async Task DeleteSessionAsync_ShouldRemoveAllConversationLogs()
    {
        // Arrange
        using var dbContext = new ApplicationDbContext(_dbContextOptions);
        var conversationMemory = new ConversationMemory(dbContext, _mockTenantAccessor.Object, _mockLogger.Object);
        var sessionId = Guid.NewGuid();
        
        // Create multiple conversation logs
        var logs = new List<ConversationLog>
        {
            new ConversationLog
            {
                SessionId = sessionId,
                UserId = _userId,
                TenantId = _tenantId,
                Query = "First query",
                QueryTimestamp = DateTime.UtcNow.AddMinutes(-10),
                SequenceNumber = 1
            },
            new ConversationLog
            {
                SessionId = sessionId,
                UserId = _userId,
                TenantId = _tenantId,
                Query = "Second query",
                QueryTimestamp = DateTime.UtcNow.AddMinutes(-5),
                SequenceNumber = 2
            }
        };
        
        dbContext.ConversationLogs.AddRange(logs);
        await dbContext.SaveChangesAsync();
        
        // Act
        var result = await conversationMemory.DeleteSessionAsync(sessionId);
        
        // Assert
        Assert.True(result);
        var remainingLogs = await dbContext.ConversationLogs.Where(c => c.SessionId == sessionId).ToListAsync();
        Assert.Empty(remainingLogs);
    }
} 