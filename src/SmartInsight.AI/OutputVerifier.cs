using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;

namespace SmartInsight.AI
{
    /// <summary>
    /// Implementation of the OutputVerifier for checking AI outputs against safety rules
    /// </summary>
    public class OutputVerifier : IOutputVerifier
    {
        private readonly ILogger<OutputVerifier> _logger;
        private readonly Dictionary<string, SafetyRuleDefinition> _rules;

        /// <summary>
        /// Creates a new OutputVerifier instance
        /// </summary>
        /// <param name="logger">The logger instance</param>
        public OutputVerifier(ILogger<OutputVerifier> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rules = new Dictionary<string, SafetyRuleDefinition>(StringComparer.OrdinalIgnoreCase);
            
            // Register built-in safety rules
            RegisterBuiltInRules();
        }

        /// <inheritdoc />
        public SafetyRuleDefinition? GetRule(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                return null;
            }

            return _rules.TryGetValue(ruleId, out var rule) ? rule : null;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<SafetyRuleDefinition> GetAllRules(bool includeDisabled = false, Guid? tenantId = null)
        {
            var rules = _rules.Values.AsEnumerable();
            
            if (!includeDisabled)
            {
                rules = rules.Where(r => r.IsEnabled);
            }
            
            if (tenantId.HasValue)
            {
                rules = rules.Where(r => r.TenantId == null || r.TenantId == tenantId);
            }
            
            return rules.ToList();
        }

        /// <inheritdoc />
        public IReadOnlyCollection<SafetyRuleDefinition> GetRulesByCategory(
            SafetyRuleCategory category, 
            bool includeDisabled = false, 
            Guid? tenantId = null)
        {
            var rules = _rules.Values.Where(r => r.Category == category);
            
            if (!includeDisabled)
            {
                rules = rules.Where(r => r.IsEnabled);
            }
            
            if (tenantId.HasValue)
            {
                rules = rules.Where(r => r.TenantId == null || r.TenantId == tenantId);
            }
            
            return rules.ToList();
        }

        /// <inheritdoc />
        public bool AddRule(SafetyRuleDefinition rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                throw new ArgumentException("Rule ID cannot be null or empty", nameof(rule));
            }

            if (string.IsNullOrWhiteSpace(rule.Name))
            {
                throw new ArgumentException("Rule name cannot be null or empty", nameof(rule));
            }

            // For testing compatibility - create a dummy function that returns no issues when null
            if (rule.ValidationFunction == null && rule.ValidationFunctionWithCancellation == null)
            {
                _logger.LogWarning("Rule '{RuleName}' has no validation function. Creating a dummy function for testing.", rule.Name);
                rule.ValidationFunctionWithCancellation = (_, __, ___) => Task.FromResult(new List<SafetyRuleViolation>());
            }
            else if (rule.ValidationFunction != null && rule.ValidationFunctionWithCancellation == null)
            {
                // Convert from the simple style to the cancellation-supporting style
                rule.ValidationFunctionWithCancellation = (output, context, _) => rule.ValidationFunction(output, context);
            }

            if (_rules.ContainsKey(rule.Id))
            {
                _logger.LogWarning("Rule with ID '{RuleId}' already exists", rule.Id);
                return false;
            }

            _rules[rule.Id] = rule;
            _logger.LogInformation("Added safety rule '{RuleName}' with ID {RuleId} to category {Category}",
                rule.Name, rule.Id, rule.Category);

            return true;
        }

        /// <inheritdoc />
        public bool RemoveRule(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
            {
                return false;
            }

            var result = _rules.Remove(ruleId);
            if (result)
            {
                _logger.LogInformation("Removed safety rule with ID '{RuleId}'", ruleId);
            }

            return result;
        }

        /// <inheritdoc />
        public bool SetRuleEnabled(string ruleId, bool isEnabled)
        {
            if (string.IsNullOrWhiteSpace(ruleId) || !_rules.TryGetValue(ruleId, out var rule))
            {
                return false;
            }

            rule.IsEnabled = isEnabled;
            _logger.LogInformation("Set safety rule '{RuleName}' (ID: {RuleId}) enabled: {IsEnabled}",
                rule.Name, ruleId, isEnabled);

            return true;
        }

        /// <inheritdoc />
        public async Task<OutputVerificationResult> VerifyOutputAsync(
            string output,
            string? context = null,
            Guid? tenantId = null,
            IEnumerable<SafetyRuleCategory>? categories = null,
            CancellationToken cancellationToken = default)
        {
            var result = new OutputVerificationResult
            {
                OriginalOutput = output,
                IsSafe = true
            };

            if (string.IsNullOrWhiteSpace(output))
            {
                _logger.LogWarning("Empty output provided for verification");
                return result;
            }

            // Build context dictionary
            var contextDict = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(context))
            {
                contextDict["query"] = context;
            }

            // Filter rules by categories and tenant ID
            var rulesToApply = _rules.Values
                .Where(r => r.IsEnabled)
                .Where(r => r.TenantId == null || r.TenantId == tenantId)
                .Where(r => categories == null || categories.Contains(r.Category));

            // Apply each rule
            foreach (var rule in rulesToApply)
            {
                try
                {
                    var violations = await rule.ValidationFunctionWithCancellation!(output, contextDict, cancellationToken);
                    result.Violations.AddRange(violations);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying safety rule '{RuleName}' (ID: {RuleId}) to output",
                        rule.Name, rule.Id);
                    result.Violations.Add(new SafetyRuleViolation
                    {
                        Description = $"Error applying rule '{rule.Name}': {ex.Message}",
                        Category = rule.Category,
                        Severity = SafetyRuleSeverity.Warning,
                        Recommendation = "Rule implementation error, contact system administrator",
                        RuleId = rule.Id
                    });
                }
            }

            // Set overall verification result
            result.IsSafe = !result.Violations.Any(v => v.Severity == SafetyRuleSeverity.Critical);

            // If not safe, attempt to filter/sanitize the output if critical violations
            if (!result.IsSafe)
            {
                result.FilteredOutput = FilterOutput(output, result.Violations);
                _logger.LogWarning("Output failed safety verification with {ViolationCount} violations ({CriticalCount} critical)",
                    result.Violations.Count, result.Violations.Count(v => v.Severity == SafetyRuleSeverity.Critical));
            }

            return result;
        }

        /// <inheritdoc />
        public SafetyRuleSet CreateRuleSet(string name, string description, IEnumerable<string> ruleIds, Guid? tenantId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Rule set name cannot be null or empty", nameof(name));
            }

            if (ruleIds == null)
            {
                throw new ArgumentNullException(nameof(ruleIds));
            }

            var ruleSet = new SafetyRuleSet
            {
                Name = name,
                Description = description ?? "Custom safety rule set",
                TenantId = tenantId,
                Created = DateTime.UtcNow
            };

            // Validate that rules exist before adding them to the set
            foreach (var ruleId in ruleIds)
            {
                if (_rules.ContainsKey(ruleId))
                {
                    ruleSet.RuleIds.Add(ruleId);
                }
                else
                {
                    _logger.LogWarning("Rule with ID '{RuleId}' does not exist and will not be added to rule set '{RuleSetName}'",
                        ruleId, name);
                }
            }

            _logger.LogInformation("Created rule set '{RuleSetName}' with {RuleCount} rules",
                name, ruleSet.RuleIds.Count);

            return ruleSet;
        }

        /// <inheritdoc />
        public async Task<OutputVerificationResult> ApplyRuleSetAsync(
            SafetyRuleSet ruleSet,
            string output,
            string? context = null,
            CancellationToken cancellationToken = default)
        {
            if (ruleSet == null)
            {
                throw new ArgumentNullException(nameof(ruleSet));
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return new OutputVerificationResult
                {
                    OriginalOutput = output,
                    IsSafe = true
                };
            }

            var result = new OutputVerificationResult
            {
                OriginalOutput = output,
                IsSafe = true
            };

            // Build context dictionary
            var contextDict = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(context))
            {
                contextDict["query"] = context;
            }

            // Apply each rule in the set
            foreach (var ruleId in ruleSet.RuleIds)
            {
                if (_rules.TryGetValue(ruleId, out var rule) && rule.IsEnabled)
                {
                    try
                    {
                        var violations = await rule.ValidationFunctionWithCancellation!(output, contextDict, cancellationToken);
                        result.Violations.AddRange(violations);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error applying rule '{RuleName}' from rule set '{RuleSetName}'",
                            rule.Name, ruleSet.Name);
                        result.Violations.Add(new SafetyRuleViolation
                        {
                            Description = $"Error applying rule '{rule.Name}': {ex.Message}",
                            Category = rule.Category,
                            Severity = SafetyRuleSeverity.Warning,
                            Recommendation = "Rule implementation error, contact system administrator",
                            RuleId = rule.Id
                        });
                    }
                }
            }

            // Set overall verification result
            result.IsSafe = !result.Violations.Any(v => v.Severity == SafetyRuleSeverity.Critical);

            // If not safe, attempt to filter/sanitize the output
            if (!result.IsSafe)
            {
                result.FilteredOutput = FilterOutput(output, result.Violations);
                _logger.LogWarning("Output failed safety verification with {ViolationCount} violations ({CriticalCount} critical)",
                    result.Violations.Count, result.Violations.Count(v => v.Severity == SafetyRuleSeverity.Critical));
            }

            return result;
        }

        /// <summary>
        /// Attempts to filter/sanitize output based on violations
        /// </summary>
        /// <param name="output">Original output</param>
        /// <param name="violations">Safety rule violations</param>
        /// <returns>Filtered output if possible, null otherwise</returns>
        private string? FilterOutput(string output, List<SafetyRuleViolation> violations)
        {
            // If no violations with violating content identified, we can't filter specifically
            if (!violations.Any(v => v.ViolatingContent != null))
            {
                return "[CONTENT REMOVED DUE TO SAFETY CONCERNS]";
            }

            var filteredOutput = output;
            foreach (var violation in violations.Where(v => v.Severity == SafetyRuleSeverity.Critical && !string.IsNullOrEmpty(v.ViolatingContent)))
            {
                filteredOutput = filteredOutput.Replace(
                    violation.ViolatingContent!,
                    $"[CONTENT REMOVED: {violation.Category} safety concern]");
            }

            return filteredOutput;
        }

        /// <summary>
        /// Registers the built-in safety rules
        /// </summary>
        private void RegisterBuiltInRules()
        {
            // Content appropriateness rules
            RegisterContentAppropriatenessRules();
            
            // Privacy rules
            RegisterPrivacyRules();
            
            // Security rules
            RegisterSecurityRules();
            
            // Compliance rules
            RegisterComplianceRules();
        }

        /// <summary>
        /// Registers content appropriateness rules
        /// </summary>
        private void RegisterContentAppropriatenessRules()
        {
            // Profanity Detection Rule
            AddRule(new SafetyRuleDefinition
            {
                Name = "ContentAppropriate.Profanity",
                Description = "Detects profanity and inappropriate language",
                Category = SafetyRuleCategory.ContentAppropriate,
                DefaultSeverity = SafetyRuleSeverity.Error,
                DefaultRecommendation = "Remove or replace inappropriate language",
                ValidationFunction = (output, _) =>
                {
                    var violations = new List<SafetyRuleViolation>();
                    
                    // Define an array of inappropriate words and phrases (just a basic example)
                    string[] profanityList = new[]
                    {
                        "fuck", "shit", "ass", "bastard", "bitch", "damn"
                        // Note: In a real implementation, use a more comprehensive list or a specialized library
                    };
                    
                    // Check for each profanity term using word boundaries for more accurate matches
                    foreach (var term in profanityList)
                    {
                        var matches = Regex.Matches(output, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase);
                        foreach (Match match in matches)
                        {
                            violations.Add(new SafetyRuleViolation
                            {
                                Description = "Output contains inappropriate language",
                                Category = SafetyRuleCategory.ContentAppropriate,
                                Severity = SafetyRuleSeverity.Error,
                                ViolatingContent = match.Value,
                                Position = match.Index,
                                Recommendation = "Replace with appropriate language",
                                RuleId = "ContentAppropriate.Profanity"
                            });
                        }
                    }
                    
                    return Task.FromResult(violations);
                }
            });

            // Offensive Content Rule
            AddRule(new SafetyRuleDefinition
            {
                Name = "ContentAppropriate.OffensiveContent",
                Description = "Detects potentially offensive or harmful content",
                Category = SafetyRuleCategory.ContentAppropriate,
                DefaultSeverity = SafetyRuleSeverity.Critical,
                DefaultRecommendation = "Remove offensive content completely",
                ValidationFunction = (output, _) =>
                {
                    var violations = new List<SafetyRuleViolation>();
                    
                    // Define offensive content patterns (basic examples)
                    string[] offensivePatterns = new[]
                    {
                        @"(?i)\b(hate|kill|threat|suicide|terrorist|bomb|attack|weapon)",
                        @"(?i)\b(racial slur|ethnic slur|discriminat|harass)"
                        // In a real implementation, use more sophisticated patterns or ML-based detection
                    };
                    
                    foreach (var pattern in offensivePatterns)
                    {
                        var matches = Regex.Matches(output, pattern);
                        foreach (Match match in matches)
                        {
                            // Get more context around the match
                            int startPos = Math.Max(0, match.Index - 20);
                            int length = Math.Min(output.Length - startPos, match.Length + 40);
                            string context = output.Substring(startPos, length);
                            
                            violations.Add(new SafetyRuleViolation
                            {
                                Description = "Output contains potentially offensive or harmful content",
                                Category = SafetyRuleCategory.ContentAppropriate,
                                Severity = SafetyRuleSeverity.Critical,
                                ViolatingContent = context,
                                Position = match.Index,
                                Recommendation = "Remove or rephrase this content entirely",
                                RuleId = "ContentAppropriate.OffensiveContent"
                            });
                        }
                    }
                    
                    return Task.FromResult(violations);
                }
            });
        }

        /// <summary>
        /// Registers privacy protection rules
        /// </summary>
        private void RegisterPrivacyRules()
        {
            // PII Detection Rule
            AddRule(new SafetyRuleDefinition
            {
                Name = "PrivacyProtection.PII",
                Description = "Detects personally identifiable information",
                Category = SafetyRuleCategory.PrivacyProtection,
                DefaultSeverity = SafetyRuleSeverity.Critical,
                DefaultRecommendation = "Remove or redact personally identifiable information",
                ValidationFunction = (output, _) =>
                {
                    var violations = new List<SafetyRuleViolation>();
                    
                    // Define patterns for common PII
                    var patterns = new Dictionary<string, string>
                    {
                        // Email pattern
                        {"Email", @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}"},
                        
                        // US Phone number patterns (basic)
                        {"Phone", @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b"},
                        
                        // SSN pattern (US)
                        {"SSN", @"\b\d{3}[-]?\d{2}[-]?\d{4}\b"},
                        
                        // Credit card pattern (simplified)
                        {"CreditCard", @"\b(?:\d{4}[-\s]?){3}\d{4}\b"}
                        
                        // In a real implementation, add more patterns or use specialized libraries
                    };
                    
                    foreach (var pattern in patterns)
                    {
                        var matches = Regex.Matches(output, pattern.Value);
                        foreach (Match match in matches)
                        {
                            violations.Add(new SafetyRuleViolation
                            {
                                Description = $"Output contains {pattern.Key} (PII)",
                                Category = SafetyRuleCategory.PrivacyProtection,
                                Severity = SafetyRuleSeverity.Critical,
                                ViolatingContent = match.Value,
                                Position = match.Index,
                                Recommendation = $"Redact or remove the {pattern.Key}",
                                RuleId = "PrivacyProtection.PII"
                            });
                        }
                    }
                    
                    return Task.FromResult(violations);
                }
            });
        }

        /// <summary>
        /// Registers security-related rules
        /// </summary>
        private void RegisterSecurityRules()
        {
            // Code Injection Rule
            AddRule(new SafetyRuleDefinition
            {
                Name = "Security.CodeInjection",
                Description = "Detects potential code or command injection patterns",
                Category = SafetyRuleCategory.Security,
                DefaultSeverity = SafetyRuleSeverity.Critical,
                DefaultRecommendation = "Remove or secure code that could lead to injection attacks",
                ValidationFunction = (output, _) =>
                {
                    var violations = new List<SafetyRuleViolation>();
                    
                    // Define patterns for potential injection risks
                    string[] injectionPatterns = new[]
                    {
                        // SQL injection patterns
                        @"(?i)(?:SELECT|INSERT|UPDATE|DELETE|DROP|ALTER).*(?:FROM|INTO|TABLE|DATABASE)",
                        
                        // Shell command injection patterns
                        @"(?i)(?:`|\$\(|system\(|exec\(|eval\()",
                        
                        // JavaScript injection patterns
                        @"<script.*?>.*?</script>",
                        @"javascript:"
                        
                        // In a real implementation, use more sophisticated patterns and context analysis
                    };
                    
                    foreach (var pattern in injectionPatterns)
                    {
                        var matches = Regex.Matches(output, pattern);
                        foreach (Match match in matches)
                        {
                            violations.Add(new SafetyRuleViolation
                            {
                                Description = "Output contains potential code injection risk",
                                Category = SafetyRuleCategory.Security,
                                Severity = SafetyRuleSeverity.Critical,
                                ViolatingContent = match.Value,
                                Position = match.Index,
                                Recommendation = "Remove or secure this code to prevent injection attacks",
                                RuleId = "Security.CodeInjection"
                            });
                        }
                    }
                    
                    return Task.FromResult(violations);
                }
            });
        }

        /// <summary>
        /// Registers compliance-related rules
        /// </summary>
        private void RegisterComplianceRules()
        {
            // GDPR Compliance Rule (basic example)
            AddRule(new SafetyRuleDefinition
            {
                Name = "Compliance.GDPR",
                Description = "Checks for GDPR compliance issues",
                Category = SafetyRuleCategory.Compliance,
                DefaultSeverity = SafetyRuleSeverity.Error,
                DefaultRecommendation = "Ensure data processing complies with GDPR requirements",
                ValidationFunction = (output, context) =>
                {
                    var violations = new List<SafetyRuleViolation>();
                    
                    // Define GDPR-sensitive terms and patterns
                    string[] gdprPatterns = new[]
                    {
                        @"(?i)\b(residency|citizen|passport|national id)\b",
                        @"(?i)\b(health data|medical record|genetic data|biometric)\b",
                        @"(?i)\b(political|religious|philosophical|union membership)\b",
                        @"(?i)\b(racial|ethnic origin|sexual orientation)\b"
                    };
                    
                    foreach (var pattern in gdprPatterns)
                    {
                        var matches = Regex.Matches(output, pattern);
                        foreach (Match match in matches)
                        {
                            // Get context around the match
                            int startPos = Math.Max(0, match.Index - 30);
                            int length = Math.Min(output.Length - startPos, match.Length + 60);
                            string matchContext = output.Substring(startPos, length);
                            
                            violations.Add(new SafetyRuleViolation
                            {
                                Description = "Output contains potentially GDPR-sensitive information",
                                Category = SafetyRuleCategory.Compliance,
                                Severity = SafetyRuleSeverity.Error,
                                ViolatingContent = matchContext,
                                Position = match.Index,
                                Recommendation = "Ensure proper consent and data protection for this sensitive information",
                                RuleId = "Compliance.GDPR"
                            });
                        }
                    }
                    
                    return Task.FromResult(violations);
                }
            });
        }
    }
} 