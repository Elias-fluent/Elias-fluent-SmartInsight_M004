using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Validators
{
    /// <summary>
    /// Specialized validator for user-related SQL queries
    /// </summary>
    public class UserParameterValidator
    {
        private readonly IParameterValidator _baseValidator;
        private readonly ILogger<UserParameterValidator> _logger;
        
        /// <summary>
        /// Creates a new instance of UserParameterValidator
        /// </summary>
        /// <param name="baseValidator">The base parameter validator</param>
        /// <param name="logger">Logger instance</param>
        public UserParameterValidator(
            IParameterValidator baseValidator,
            ILogger<UserParameterValidator> logger)
        {
            _baseValidator = baseValidator ?? throw new ArgumentNullException(nameof(baseValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Validates user-specific parameters
        /// </summary>
        /// <param name="parameters">The parameters to validate</param>
        /// <param name="template">The SQL template</param>
        /// <returns>The validation result</returns>
        public async Task<Models.ParameterValidationResult> ValidateUserParametersAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template)
        {
            // Start with base validation
            var result = await _baseValidator.ValidateParametersAsync(parameters, template);
            
            // If already invalid, no need for additional validation
            if (!result.IsValid)
            {
                return result;
            }
            
            // Add specialized validation for user parameters
            if (parameters.TryGetValue("email", out var emailParam))
            {
                if (emailParam.Value is string email)
                {
                    // Custom email validation (domain-specific)
                    if (email.EndsWith("@example.com", StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddIssue(new ParameterValidationIssue
                        {
                            ParameterName = "email",
                            RuleName = "Business.RestrictedDomain",
                            Description = "Email addresses from example.com domain are not allowed",
                            Severity = ValidationSeverity.Critical,
                            OriginalValue = email,
                            Recommendation = "Provide an email address from an allowed domain"
                        });
                    }
                }
            }
            
            if (parameters.TryGetValue("username", out var usernameParam))
            {
                if (usernameParam.Value is string username)
                {
                    // Custom username validation
                    if (username.Length < 5)
                    {
                        result.AddIssue(new ParameterValidationIssue
                        {
                            ParameterName = "username",
                            RuleName = "Business.UsernameLength",
                            Description = "Username must be at least 5 characters long",
                            Severity = ValidationSeverity.Critical,
                            OriginalValue = username,
                            Recommendation = "Provide a username with at least 5 characters"
                        });
                    }
                    
                    // Username format validation
                    if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$"))
                    {
                        result.AddIssue(new ParameterValidationIssue
                        {
                            ParameterName = "username",
                            RuleName = "Business.UsernameFormat",
                            Description = "Username must contain only letters, numbers, underscores, and hyphens",
                            Severity = ValidationSeverity.Critical,
                            OriginalValue = username,
                            Recommendation = "Remove special characters from the username"
                        });
                    }
                }
            }
            
            if (parameters.TryGetValue("role", out var roleParam))
            {
                if (roleParam.Value is string role)
                {
                    // Only allow specific roles
                    var allowedRoles = new[] { "user", "admin", "manager", "readonly" };
                    if (!allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                    {
                        result.AddIssue(new ParameterValidationIssue
                        {
                            ParameterName = "role",
                            RuleName = "Business.AllowedRole",
                            Description = $"Role '{role}' is not allowed",
                            Severity = ValidationSeverity.Critical,
                            OriginalValue = role,
                            Recommendation = $"Provide one of the allowed roles: {string.Join(", ", allowedRoles)}"
                        });
                    }
                }
            }
            
            // Check for reserved usernames
            if (parameters.TryGetValue("username", out var usernameParam2))
            {
                if (usernameParam2.Value is string username2)
                {
                    var reservedUsernames = new[] { "admin", "system", "root", "superuser" };
                    if (reservedUsernames.Contains(username2, StringComparer.OrdinalIgnoreCase))
                    {
                        result.AddIssue(new ParameterValidationIssue
                        {
                            ParameterName = "username",
                            RuleName = "Business.ReservedUsername",
                            Description = $"Username '{username2}' is reserved",
                            Severity = ValidationSeverity.Critical,
                            OriginalValue = username2,
                            Recommendation = "Choose a different username"
                        });
                    }
                }
            }
            
            return result;
        }
    }
} 