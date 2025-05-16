using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SmartInsight.AI;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;
using Xunit;

namespace SmartInsight.Tests.AI.Unit
{
    public class OutputVerifierTests
    {
        private readonly Mock<ILogger<OutputVerifier>> _mockLogger;
        private readonly IOutputVerifier _outputVerifier;

        public OutputVerifierTests()
        {
            _mockLogger = new Mock<ILogger<OutputVerifier>>();
            _outputVerifier = new OutputVerifier(_mockLogger.Object);
        }

        [Fact]
        public async Task VerifyOutputAsync_WithSafeContent_ReturnsSafeResult()
        {
            // Arrange
            string safeContent = "This is a safe piece of content without any violations.";
            
            // Act
            var result = await _outputVerifier.VerifyOutputAsync(safeContent);
            
            // Assert
            Assert.True(result.IsSafe);
            Assert.Empty(result.Violations);
            Assert.Null(result.FilteredOutput);
            Assert.False(result.WasModified);
            Assert.Equal(safeContent, result.OriginalOutput);
        }

        [Fact]
        public async Task VerifyOutputAsync_WithProfanity_ReturnsProfanityViolation()
        {
            // Arrange
            string contentWithProfanity = "This content contains the word damn which is flagged.";
            
            // Act
            var result = await _outputVerifier.VerifyOutputAsync(contentWithProfanity);
            
            // Assert
            Assert.True(result.Violations.Any(v => 
                v.Category == SafetyRuleCategory.ContentAppropriate && 
                v.RuleId == "ContentAppropriate.Profanity"));
            
            // Since profanity is Error level, not Critical, output is still safe but violations are recorded
            Assert.True(result.IsSafe);
            Assert.Equal(contentWithProfanity, result.OriginalOutput);
            Assert.Null(result.FilteredOutput);
        }

        [Fact]
        public async Task VerifyOutputAsync_WithPII_ReturnsUnsafeResultWithFilter()
        {
            // Arrange
            string contentWithPII = "My email is test@example.com and my phone is 555-123-4567.";
            
            // Act
            var result = await _outputVerifier.VerifyOutputAsync(contentWithPII);
            
            // Assert
            Assert.False(result.IsSafe);
            Assert.NotEmpty(result.Violations);
            Assert.True(result.Violations.Any(v => 
                v.Category == SafetyRuleCategory.PrivacyProtection && 
                v.RuleId == "PrivacyProtection.PII"));
            Assert.NotNull(result.FilteredOutput);
            Assert.True(result.WasModified);
            
            // Verify both PII elements are detected
            Assert.Contains(result.Violations, v => v.ViolatingContent.Contains("test@example.com"));
            Assert.Contains(result.Violations, v => v.ViolatingContent.Contains("555-123-4567"));
            
            // Verify filtered output has redacted content
            Assert.Contains("[CONTENT REMOVED:", result.FilteredOutput);
        }

        [Fact]
        public async Task VerifyOutputAsync_WithSQLInjection_ReturnsSecurityViolation()
        {
            // Arrange
            string contentWithInjection = "You can try this SQL: SELECT * FROM users WHERE username = 'admin';";
            
            // Act
            var result = await _outputVerifier.VerifyOutputAsync(contentWithInjection);
            
            // Assert
            Assert.False(result.IsSafe);
            Assert.NotEmpty(result.Violations);
            Assert.True(result.Violations.Any(v => 
                v.Category == SafetyRuleCategory.Security && 
                v.RuleId == "Security.CodeInjection"));
            Assert.NotNull(result.FilteredOutput);
            Assert.True(result.WasModified);
            
            // Verify SQL is detected
            Assert.Contains(result.Violations, v => v.ViolatingContent.Contains("SELECT * FROM users"));
        }

        [Fact]
        public async Task VerifyOutputAsync_WithEmptyContent_ReturnsSafeResult()
        {
            // Arrange
            string emptyContent = "";
            
            // Act
            var result = await _outputVerifier.VerifyOutputAsync(emptyContent);
            
            // Assert
            Assert.True(result.IsSafe);
            Assert.Empty(result.Violations);
            Assert.Equal(emptyContent, result.OriginalOutput);
        }

        [Fact]
        public async Task VerifyOutputAsync_WithMultipleViolations_ReturnsAllViolations()
        {
            // Arrange
            string contentWithMultipleIssues = 
                "My email is test@example.com. Here's some SQL: SELECT * FROM users; " +
                "Also, this content has the word shit in it.";
            
            // Act
            var result = await _outputVerifier.VerifyOutputAsync(contentWithMultipleIssues);
            
            // Assert
            Assert.False(result.IsSafe);
            
            // Should have at least 3 violations of different types
            Assert.True(result.Violations.Any(v => v.Category == SafetyRuleCategory.PrivacyProtection));
            Assert.True(result.Violations.Any(v => v.Category == SafetyRuleCategory.Security));
            Assert.True(result.Violations.Any(v => v.Category == SafetyRuleCategory.ContentAppropriate));
            
            Assert.NotNull(result.FilteredOutput);
            Assert.True(result.WasModified);
        }

        [Fact]
        public void AddRule_WithValidRule_AddsRuleSuccessfully()
        {
            // Arrange
            var rule = new SafetyRuleDefinition
            {
                Name = "Test.CustomRule",
                Description = "A custom test rule",
                Category = SafetyRuleCategory.Security,
                DefaultSeverity = SafetyRuleSeverity.Warning,
                ValidationFunction = (_, __) => Task.FromResult(new List<SafetyRuleViolation>())
            };
            
            // Act
            bool result = _outputVerifier.AddRule(rule);
            var retrievedRule = _outputVerifier.GetRule(rule.Id);
            
            // Assert
            Assert.True(result);
            Assert.NotNull(retrievedRule);
            Assert.Equal(rule.Name, retrievedRule.Name);
        }

        [Fact]
        public void AddRule_WithDuplicateId_ReturnsFalse()
        {
            // Arrange
            var rule = new SafetyRuleDefinition
            {
                Id = "duplicateId",
                Name = "Test.Rule1",
                Description = "First test rule",
                Category = SafetyRuleCategory.Security,
                ValidationFunction = (_, __) => Task.FromResult(new List<SafetyRuleViolation>())
            };
            
            var duplicateRule = new SafetyRuleDefinition
            {
                Id = "duplicateId",
                Name = "Test.Rule2",
                Description = "Second test rule with the same ID",
                Category = SafetyRuleCategory.Security,
                ValidationFunction = (_, __) => Task.FromResult(new List<SafetyRuleViolation>())
            };
            
            // Act
            bool firstResult = _outputVerifier.AddRule(rule);
            bool secondResult = _outputVerifier.AddRule(duplicateRule);
            
            // Assert
            Assert.True(firstResult);
            Assert.False(secondResult);
        }

        [Fact]
        public void GetRulesByCategory_ReturnsCorrectRules()
        {
            // Arrange
            var securityRule = new SafetyRuleDefinition
            {
                Name = "Test.SecurityRule",
                Description = "A security test rule",
                Category = SafetyRuleCategory.Security,
                ValidationFunction = (_, __) => Task.FromResult(new List<SafetyRuleViolation>())
            };
            
            var privacyRule = new SafetyRuleDefinition
            {
                Name = "Test.PrivacyRule",
                Description = "A privacy test rule",
                Category = SafetyRuleCategory.PrivacyProtection,
                ValidationFunction = (_, __) => Task.FromResult(new List<SafetyRuleViolation>())
            };
            
            _outputVerifier.AddRule(securityRule);
            _outputVerifier.AddRule(privacyRule);
            
            // Act
            var securityRules = _outputVerifier.GetRulesByCategory(SafetyRuleCategory.Security, true);
            var privacyRules = _outputVerifier.GetRulesByCategory(SafetyRuleCategory.PrivacyProtection, true);
            
            // Assert
            Assert.Contains(securityRules, r => r.Id == securityRule.Id);
            Assert.DoesNotContain(securityRules, r => r.Id == privacyRule.Id);
            
            Assert.Contains(privacyRules, r => r.Id == privacyRule.Id);
            Assert.DoesNotContain(privacyRules, r => r.Id == securityRule.Id);
        }

        [Fact]
        public async Task ApplyRuleSetAsync_AppliesOnlyRulesInSet()
        {
            // Arrange
            var customRule = new SafetyRuleDefinition
            {
                Name = "Test.CustomRule",
                Description = "A test rule that always finds a violation",
                Category = SafetyRuleCategory.Security,
                DefaultSeverity = SafetyRuleSeverity.Critical,
                ValidationFunction = (_, __) => Task.FromResult(new List<SafetyRuleViolation>
                {
                    new SafetyRuleViolation
                    {
                        Description = "Test violation",
                        Category = SafetyRuleCategory.Security,
                        Severity = SafetyRuleSeverity.Critical,
                        RuleId = "Test.CustomRule"
                    }
                })
            };
            
            _outputVerifier.AddRule(customRule);
            
            var ruleSet = _outputVerifier.CreateRuleSet(
                "TestRuleSet", 
                "A test rule set", 
                new[] { customRule.Id });
            
            // Act - apply only the ruleset which includes our custom rule
            string safeContent = "This should trigger our custom rule only";
            var result = await _outputVerifier.ApplyRuleSetAsync(ruleSet, safeContent);
            
            // Assert
            Assert.False(result.IsSafe);
            Assert.Single(result.Violations);
            Assert.Equal("Test.CustomRule", result.Violations.First().RuleId);
            Assert.Equal(SafetyRuleCategory.Security, result.Violations.First().Category);
            Assert.Equal(SafetyRuleSeverity.Critical, result.Violations.First().Severity);
        }
    }
} 