using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;
using SmartInsight.Tests.SQL.Common.TestData;
using SmartInsight.Tests.SQL.Common.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Unit.Validators
{
    /// <summary>
    /// Unit tests for SqlValidationRulesEngine
    /// </summary>
    public class SqlValidationRulesEngineTests : SqlTestBase
    {
        private readonly ISqlValidationRulesEngine _rulesEngine;

        public SqlValidationRulesEngineTests(ITestOutputHelper output) : base(output)
        {
            _rulesEngine = _serviceProvider.GetRequiredService<ISqlValidationRulesEngine>();
        }

        [Fact]
        public void GetAllRules_ReturnsRules()
        {
            // Act
            var rules = _rulesEngine.GetAllRules();

            // Assert
            Assert.NotNull(rules);
            Assert.NotEmpty(rules);
        }

        [Fact]
        public void GetRulesByCategory_WithSecurityCategory_ReturnsSecurityRules()
        {
            // Act
            var securityRules = _rulesEngine.GetRulesByCategory(ValidationCategory.Security);

            // Assert
            Assert.NotNull(securityRules);
            Assert.All(securityRules, rule => Assert.Equal("Security", rule.Category));
        }

        [Fact]
        public void AddRule_WithUniqueRule_ReturnsTrue()
        {
            // Arrange
            var uniqueRuleName = $"TestRule_{Guid.NewGuid()}";
            var rule = new SqlValidationRuleDefinition
            {
                Name = uniqueRuleName,
                Description = "Test Rule",
                Category = "Syntax",
                CategoryEnum = ValidationCategory.Syntax,
                DefaultSeverity = ValidationSeverity.Info,
                DefaultRecommendation = "Test recommendation",
                IsEnabled = true
            };

            // Act
            var result = _rulesEngine.AddRule(rule);
            var addedRule = _rulesEngine.GetRule(uniqueRuleName);

            // Assert
            Assert.True(result);
            Assert.NotNull(addedRule);
            Assert.Equal(uniqueRuleName, addedRule.Name);
        }

        [Fact]
        public void AddRule_WithDuplicateRule_ReturnsFalse()
        {
            // Arrange
            var rules = _rulesEngine.GetAllRules();
            if (rules.Count == 0)
            {
                // If no rules exist, add one first
                var uniqueRuleName = $"TestRule_{Guid.NewGuid()}";
                var newRule = new SqlValidationRuleDefinition
                {
                    Name = uniqueRuleName,
                    Description = "Test Rule",
                    Category = "Syntax",
                    CategoryEnum = ValidationCategory.Syntax,
                    DefaultSeverity = ValidationSeverity.Info,
                    DefaultRecommendation = "Test recommendation",
                    IsEnabled = true
                };
                _rulesEngine.AddRule(newRule);
                rules = _rulesEngine.GetAllRules();
            }

            var existingRule = rules.First();
            var duplicateRule = new SqlValidationRuleDefinition
            {
                Name = existingRule.Name,
                Description = "Duplicate Test Rule",
                Category = "Performance",
                CategoryEnum = ValidationCategory.Performance,
                DefaultSeverity = ValidationSeverity.Warning,
                DefaultRecommendation = "Duplicate recommendation",
                IsEnabled = true
            };

            // Act
            var result = _rulesEngine.AddRule(duplicateRule);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RemoveRule_WithExistingRule_ReturnsTrue()
        {
            // Arrange
            var uniqueRuleName = $"TestRuleToRemove_{Guid.NewGuid()}";
            var rule = new SqlValidationRuleDefinition
            {
                Name = uniqueRuleName,
                Description = "Test Rule to Remove",
                Category = "Syntax",
                CategoryEnum = ValidationCategory.Syntax,
                DefaultSeverity = ValidationSeverity.Info,
                DefaultRecommendation = "Remove recommendation",
                IsEnabled = true
            };
            _rulesEngine.AddRule(rule);

            // Act
            var result = _rulesEngine.RemoveRule(uniqueRuleName);
            var removedRule = _rulesEngine.GetRule(uniqueRuleName);

            // Assert
            Assert.True(result);
            Assert.Null(removedRule);
        }

        [Fact]
        public void RemoveRule_WithNonExistingRule_ReturnsFalse()
        {
            // Arrange
            var nonExistingRuleName = $"NonExistingRule_{Guid.NewGuid()}";

            // Act
            var result = _rulesEngine.RemoveRule(nonExistingRuleName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SetRuleEnabled_WithExistingRule_ReturnsTrue()
        {
            // Arrange
            var uniqueRuleName = $"TestRuleToToggle_{Guid.NewGuid()}";
            var rule = new SqlValidationRuleDefinition
            {
                Name = uniqueRuleName,
                Description = "Test Rule to Toggle",
                Category = "Syntax",
                CategoryEnum = ValidationCategory.Syntax,
                DefaultSeverity = ValidationSeverity.Info,
                DefaultRecommendation = "Toggle recommendation",
                IsEnabled = true
            };
            _rulesEngine.AddRule(rule);

            // Act - Disable
            var disableResult = _rulesEngine.SetRuleEnabled(uniqueRuleName, false);
            var disabledRule = _rulesEngine.GetRule(uniqueRuleName);

            // Act - Enable
            var enableResult = _rulesEngine.SetRuleEnabled(uniqueRuleName, true);
            var enabledRule = _rulesEngine.GetRule(uniqueRuleName);

            // Assert
            Assert.True(disableResult);
            Assert.False(disabledRule?.IsEnabled);
            Assert.True(enableResult);
            Assert.True(enabledRule?.IsEnabled);
        }

        [Fact]
        public async Task ValidateSqlAsync_WithValidSql_ReturnsValidResult()
        {
            // Arrange
            string validSql = "SELECT Id, Name FROM Users WHERE Id = @userId";

            // Act
            var result = await _rulesEngine.ValidateSqlAsync(validSql);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid || !result.Issues.Any(i => i.Severity == ValidationSeverity.Critical));
        }

        [Fact]
        public async Task ValidateSqlAsync_WithSelectStar_DetectsSelectStarRule()
        {
            // Arrange
            string sqlWithSelectStar = "SELECT * FROM Users";
            
            // Ensure we have the select star rule
            var selectStarRule = new SqlValidationRuleDefinition
            {
                Name = "NoSelectStar",
                Description = "Avoid using SELECT * in queries",
                Category = "Performance",
                CategoryEnum = ValidationCategory.Performance,
                DefaultSeverity = ValidationSeverity.Warning,
                DefaultRecommendation = "Specify the columns you need",
                IsEnabled = true
            };
            
            // Try to add the rule (it's ok if it already exists)
            _rulesEngine.AddRule(selectStarRule);

            // Act
            var result = await _rulesEngine.ValidateSqlAsync(
                sqlWithSelectStar, 
                null, 
                new List<ValidationCategory> { ValidationCategory.Performance });

            // Assert
            Assert.NotNull(result);
            // It may still be valid (not critical) but should have the performance issue
            Assert.Contains(result.Issues, 
                issue => issue.Category == ValidationCategory.Performance && 
                         issue.Description.Contains("SELECT *", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ValidateTemplateAsync_WithValidTemplate_ReturnsValidResult()
        {
            // Arrange
            var template = CreateSampleTemplate(
                "test-template", 
                "Valid Template", 
                "SELECT Id, Name FROM Users WHERE Id = @userId");

            // Act
            var result = await _rulesEngine.ValidateTemplateAsync(template);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid || !result.Issues.Any(i => i.Severity == ValidationSeverity.Critical));
        }

        [Fact]
        public async Task ValidateTemplateAsync_WithUnsafeTemplate_DetectsIssues()
        {
            // Arrange
            var unsafeTemplate = CreateSampleTemplate(
                "unsafe-template", 
                "Unsafe Template", 
                "SELECT * FROM Users; DROP TABLE Logs; --");

            // Act
            var result = await _rulesEngine.ValidateTemplateAsync(
                unsafeTemplate, 
                new List<ValidationCategory> { ValidationCategory.Security });

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result.Issues, issue => issue.Category == ValidationCategory.Security);
        }
    }
} 