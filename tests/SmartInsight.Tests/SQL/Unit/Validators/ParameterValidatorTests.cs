using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;
using SmartInsight.Tests.SQL.Common.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Unit.Validators
{
    /// <summary>
    /// Unit tests for ParameterValidator
    /// </summary>
    public class ParameterValidatorTests : SqlTestBase
    {
        private readonly IParameterValidator _parameterValidator;

        public ParameterValidatorTests(ITestOutputHelper output) : base(output)
        {
            _parameterValidator = _serviceProvider.GetRequiredService<IParameterValidator>();
        }

        [Fact]
        public async Task ValidateParametersAsync_WithValidParameters_ReturnsValid()
        {
            // Arrange
            var template = CreateTemplateWithParameters();
            var parameters = new Dictionary<string, ExtractedParameter>
            {
                { "userId", new ExtractedParameter { Name = "userId", Value = 1, Type = "Integer", Confidence = 0.9 } },
                { "minOrderDate", new ExtractedParameter { Name = "minOrderDate", Value = DateTime.UtcNow.AddDays(-30), Type = "DateTime", Confidence = 0.8 } },
                { "status", new ExtractedParameter { Name = "status", Value = "active", Type = "String", Confidence = 0.95 } }
            };

            // Act
            var result = await _parameterValidator.ValidateParametersAsync(parameters, template);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public async Task ValidateParametersAsync_WithMissingRequiredParameter_ReturnsInvalid()
        {
            // Arrange
            var template = CreateTemplateWithParameters();
            var parameters = new Dictionary<string, ExtractedParameter>
            {
                // Missing userId which is required
                { "minOrderDate", new ExtractedParameter { Name = "minOrderDate", Value = DateTime.UtcNow.AddDays(-30), Type = "DateTime", Confidence = 0.8 } },
                { "status", new ExtractedParameter { Name = "status", Value = "active", Type = "String", Confidence = 0.95 } }
            };

            // Act
            var result = await _parameterValidator.ValidateParametersAsync(parameters, template);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Issues);
            Assert.Contains(result.Issues, issue => issue.ParameterName == "userId" && issue.Severity == ValidationSeverity.Critical);
        }

        [Fact]
        public async Task ValidateParametersAsync_WithInvalidParameterType_ReturnsInvalid()
        {
            // Arrange
            var template = CreateTemplateWithParameters();
            var parameters = new Dictionary<string, ExtractedParameter>
            {
                { "userId", new ExtractedParameter { Name = "userId", Value = "not-an-integer", Type = "String", Confidence = 0.9 } },
                { "minOrderDate", new ExtractedParameter { Name = "minOrderDate", Value = DateTime.UtcNow.AddDays(-30), Type = "DateTime", Confidence = 0.8 } },
                { "status", new ExtractedParameter { Name = "status", Value = "active", Type = "String", Confidence = 0.95 } }
            };

            // Act
            var result = await _parameterValidator.ValidateParametersAsync(parameters, template);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Issues);
            Assert.Contains(result.Issues, issue => issue.ParameterName == "userId" && issue.Issue.Contains("type", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ValidateParametersAsync_WithSqlInjectionAttempt_ReturnsInvalid()
        {
            // Arrange
            var template = CreateTemplateWithParameters();
            var parameters = new Dictionary<string, ExtractedParameter>
            {
                { "userId", new ExtractedParameter { Name = "userId", Value = 1, Type = "Integer", Confidence = 0.9 } },
                { "minOrderDate", new ExtractedParameter { Name = "minOrderDate", Value = DateTime.UtcNow.AddDays(-30), Type = "DateTime", Confidence = 0.8 } },
                { "status", new ExtractedParameter { Name = "status", Value = "active'; DROP TABLE Orders; --", Type = "String", Confidence = 0.95 } }
            };

            // Act
            var result = await _parameterValidator.ValidateParametersAsync(parameters, template);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Issues);
            Assert.Contains(result.Issues, issue => 
                issue.ParameterName == "status" && 
                issue.Severity == ValidationSeverity.Critical &&
                issue.Issue.Contains("injection", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ValidateParameterAsync_WithValidParameter_ReturnsNull()
        {
            // Arrange
            var templateParameter = new SqlTemplateParameter
            {
                Name = "price",
                Type = "Decimal",
                Required = true,
                Description = "Product price",
                MinValue = "0.01",
                MaxValue = "9999.99"
            };
            
            var parameter = new ExtractedParameter
            {
                Name = "price",
                Value = 42.50m,
                Type = "Decimal",
                Confidence = 0.9
            };

            // Act
            var issue = await _parameterValidator.ValidateParameterAsync(parameter, templateParameter, "ValueRange");

            // Assert
            Assert.Null(issue);
        }

        [Fact]
        public async Task ValidateParameterAsync_WithOutOfRangeValue_ReturnsIssue()
        {
            // Arrange
            var templateParameter = new SqlTemplateParameter
            {
                Name = "price",
                Type = "Decimal",
                Required = true,
                Description = "Product price",
                MinValue = "0.01",
                MaxValue = "9999.99"
            };
            
            var parameter = new ExtractedParameter
            {
                Name = "price",
                Value = -5.00m,
                Type = "Decimal",
                Confidence = 0.9
            };

            // Act
            var issue = await _parameterValidator.ValidateParameterAsync(parameter, templateParameter, "ValueRange");

            // Assert
            Assert.NotNull(issue);
            Assert.Equal("price", issue.ParameterName);
            Assert.Contains("range", issue.Issue, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetAvailableRules_ReturnsNonEmptyList()
        {
            // Act
            var rules = _parameterValidator.GetAvailableRules();

            // Assert
            Assert.NotNull(rules);
            Assert.NotEmpty(rules);
            
            _output.WriteLine($"Available validation rules:");
            foreach (var rule in rules)
            {
                _output.WriteLine($"- {rule.Name}: {rule.Description} (Severity: {rule.Severity})");
            }
        }

        [Fact]
        public void RegisterValidationRule_WithUniqueRule_AddsRule()
        {
            // Arrange
            string uniqueRuleName = $"TestRule_{Guid.NewGuid()}";
            
            // Act
            _parameterValidator.RegisterValidationRule(
                uniqueRuleName,
                "Test validation rule",
                ValidationSeverity.Warning,
                new[] { "String", "Integer" });
            
            var rules = _parameterValidator.GetAvailableRules();
            
            // Assert
            Assert.Contains(rules, r => r.Name == uniqueRuleName);
        }

        private SqlTemplate CreateTemplateWithParameters()
        {
            return new SqlTemplate
            {
                Id = "test-template",
                Name = "Test Template",
                Description = "Template for testing parameter validation",
                SqlTemplateText = "SELECT * FROM Orders WHERE UserId = @userId AND (@minOrderDate IS NULL OR OrderDate >= @minOrderDate) AND Status = @status",
                Parameters = new List<SqlTemplateParameter>
                {
                    new SqlTemplateParameter
                    {
                        Name = "userId",
                        Type = "Integer",
                        Required = true,
                        Description = "User ID to filter by"
                    },
                    new SqlTemplateParameter
                    {
                        Name = "minOrderDate",
                        Type = "DateTime",
                        Required = false,
                        Description = "Minimum order date to filter by"
                    },
                    new SqlTemplateParameter
                    {
                        Name = "status",
                        Type = "String",
                        Required = true,
                        Description = "Order status",
                        AllowedValues = new List<string> { "pending", "processing", "shipped", "delivered", "cancelled", "active" }
                    }
                },
                Created = DateTime.UtcNow,
                Version = "1.0"
            };
        }
    }
} 