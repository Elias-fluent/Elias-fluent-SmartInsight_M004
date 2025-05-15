using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Implementation of IParameterExtractor that extracts parameters from natural language queries.
    /// </summary>
    public class ParameterExtractor : IParameterExtractor
    {
        private readonly ILogger<ParameterExtractor> _logger;
        private readonly IOllamaClient? _ollamaClient;
        private readonly Dictionary<string, Func<string, object?>> _typeConverters;
        private const double DEFAULT_CONFIDENCE_THRESHOLD = 0.7;

        /// <summary>
        /// Creates a new instance of ParameterExtractor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="ollamaClient">Optional Ollama client for advanced extraction</param>
        public ParameterExtractor(ILogger<ParameterExtractor> logger, IOllamaClient? ollamaClient = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ollamaClient = ollamaClient;
            _typeConverters = InitializeTypeConverters();
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, ExtractedParameter>> ExtractParametersAsync(
            string query,
            SqlTemplate template,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Extracting parameters from query: {Query}", query);

            var result = new Dictionary<string, ExtractedParameter>();

            // Try different extraction strategies in order of preference
            result = await TryNamedEntityExtractionAsync(query, template, result, cancellationToken);
            result = await TryPatternMatchingExtractionAsync(query, template, result, cancellationToken);
            
            // If we have Ollama client and still missing required parameters, try advanced extraction
            if (_ollamaClient != null && template.Parameters.Any(p => p.Required && !result.ContainsKey(p.Name)))
            {
                result = await TryLlmExtractionAsync(query, template, result, cancellationToken);
            }

            // Add default values for missing optional parameters
            foreach (var param in template.Parameters.Where(p => !p.Required && !result.ContainsKey(p.Name) && p.DefaultValue != null))
            {
                result.Add(param.Name, new ExtractedParameter
                {
                    Name = param.Name,
                    Value = param.DefaultValue!,
                    Confidence = 1.0, // High confidence since it's a default
                    OriginalText = null // No original text since it's a default
                });
            }

            _logger.LogDebug("Extracted {Count} parameters from query", result.Count);
            return result;
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, ExtractedParameter>> ExtractParametersAsync(
            IntentDetectionResult intentResult,
            SqlTemplate template,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Extracting parameters from intent result for intent: {Intent}", intentResult.TopIntent);

            var result = new Dictionary<string, ExtractedParameter>();

            // Extract from entities in the intent result
            if (intentResult.Entities != null && intentResult.Entities.Count > 0)
            {
                foreach (var param in template.Parameters)
                {
                    // Look for entity with matching name or alias
                    var matchingEntity = intentResult.Entities.FirstOrDefault(e => 
                        string.Equals(e.Type, param.Name, StringComparison.OrdinalIgnoreCase) ||
                        e.Type.EndsWith($":{param.Name}", StringComparison.OrdinalIgnoreCase));

                    if (matchingEntity != null)
                    {
                        var convertedValue = ConvertToType(matchingEntity.Value?.ToString(), param.Type);
                        if (convertedValue != null)
                        {
                            result.Add(param.Name, new ExtractedParameter
                            {
                                Name = param.Name,
                                Value = convertedValue,
                                Confidence = matchingEntity.Confidence,
                                OriginalText = matchingEntity.Text
                            });
                        }
                    }
                }
            }

            // If we're still missing parameters, try extracting from the original query
            if (template.Parameters.Any(p => p.Required && !result.ContainsKey(p.Name)))
            {
                var missingParams = await ExtractParametersAsync(intentResult.Query, template, cancellationToken);
                foreach (var param in missingParams)
                {
                    if (!result.ContainsKey(param.Key))
                    {
                        result.Add(param.Key, param.Value);
                    }
                }
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, ExtractedParameter>> ExtractParametersAsync(
            ReasoningResult reasoningResult,
            SqlTemplate template,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Extracting parameters from reasoning result");

            var result = new Dictionary<string, ExtractedParameter>();

            // Extract from entities in the reasoning result
            if (reasoningResult.Entities != null && reasoningResult.Entities.Count > 0)
            {
                foreach (var param in template.Parameters)
                {
                    // Look for entity with matching name or alias
                    var matchingEntity = reasoningResult.Entities.FirstOrDefault(e => 
                        string.Equals(e.Type, param.Name, StringComparison.OrdinalIgnoreCase) ||
                        e.Type.EndsWith($":{param.Name}", StringComparison.OrdinalIgnoreCase));

                    if (matchingEntity != null)
                    {
                        var convertedValue = ConvertToType(matchingEntity.Value?.ToString(), param.Type);
                        if (convertedValue != null)
                        {
                            result.Add(param.Name, new ExtractedParameter
                            {
                                Name = param.Name,
                                Value = convertedValue,
                                Confidence = matchingEntity.Confidence,
                                OriginalText = matchingEntity.Text
                            });
                        }
                    }
                }
            }

            // Check in the structured data fields for parameter values
            if (reasoningResult.StructuredData != null)
            {
                foreach (var param in template.Parameters)
                {
                    if (!result.ContainsKey(param.Name) && 
                        reasoningResult.StructuredData.TryGetValue(param.Name, out var value))
                    {
                        var convertedValue = ConvertToType(value?.ToString(), param.Type);
                        if (convertedValue != null)
                        {
                            result.Add(param.Name, new ExtractedParameter
                            {
                                Name = param.Name,
                                Value = convertedValue,
                                Confidence = 0.9, // High confidence since it's explicitly in structured data
                                OriginalText = value?.ToString()
                            });
                        }
                    }
                }
            }

            // If we're still missing parameters, try extracting from the original query
            if (template.Parameters.Any(p => p.Required && !result.ContainsKey(p.Name)))
            {
                var missingParams = await ExtractParametersAsync(reasoningResult.Query, template, cancellationToken);
                foreach (var param in missingParams)
                {
                    if (!result.ContainsKey(param.Key))
                    {
                        result.Add(param.Key, param.Value);
                    }
                }
            }

            return result;
        }

        /// <inheritdoc />
        public Task<SmartInsight.AI.SQL.Interfaces.ParameterValidationResult> ValidateParametersAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template)
        {
            var result = new SmartInsight.AI.SQL.Interfaces.ParameterValidationResult
            {
                IsValid = true
            };

            // Check for missing required parameters
            foreach (var param in template.Parameters.Where(p => p.Required))
            {
                if (!parameters.ContainsKey(param.Name))
                {
                    result.IsValid = false;
                    result.MissingParameters.Add(param.Name);
                }
            }

            // Check for invalid parameter values
            foreach (var param in parameters)
            {
                var templateParam = template.Parameters.FirstOrDefault(p => p.Name == param.Key);
                if (templateParam == null)
                {
                    // Parameter not defined in template
                    continue;
                }

                // Check type compatibility
                if (!IsTypeCompatible(param.Value.Value, templateParam.Type))
                {
                    result.IsValid = false;
                    result.InvalidParameters.Add(param.Key, $"Value is not compatible with type {templateParam.Type}");
                }

                // Check confidence threshold
                if (param.Value.Confidence < DEFAULT_CONFIDENCE_THRESHOLD)
                {
                    result.LowConfidenceParameters.Add(param.Key, param.Value.Confidence);
                }
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<Dictionary<string, object>> ConvertToSqlParametersAsync(
            Dictionary<string, ExtractedParameter> parameters,
            SqlTemplate template)
        {
            _logger.LogDebug("Converting extracted parameters to SQL parameters");
            
            var result = new Dictionary<string, object>();
            
            foreach (var param in parameters)
            {
                var templateParam = template.Parameters.FirstOrDefault(p => p.Name == param.Key);
                if (templateParam == null)
                {
                    // Parameter not defined in template, skip it
                    _logger.LogWarning("Parameter {ParamName} not defined in template, skipping", param.Key);
                    continue;
                }
                
                // Convert the value to the correct type if needed
                var convertedValue = param.Value.Value;
                if (convertedValue != null && !IsTypeCompatible(convertedValue, templateParam.Type))
                {
                    convertedValue = ConvertToType(convertedValue.ToString(), templateParam.Type);
                    
                    if (convertedValue == null)
                    {
                        _logger.LogWarning(
                            "Could not convert parameter {ParamName} value to required type {ParamType}", 
                            param.Key, templateParam.Type);
                        continue;
                    }
                }
                
                // Add parameter with @ prefix for SQL
                if (convertedValue != null)
                {
                    result.Add(param.Key, convertedValue);
                }
                else
                {
                    _logger.LogWarning("Parameter {ParamName} has null value, skipping", param.Key);
                }
            }
            
            _logger.LogDebug("Converted {Count} parameters to SQL parameters", result.Count);
            return Task.FromResult(result);
        }

        #region Private Implementation Methods

        private Task<Dictionary<string, ExtractedParameter>> TryNamedEntityExtractionAsync(
            string query,
            SqlTemplate template,
            Dictionary<string, ExtractedParameter> existingParams,
            CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, ExtractedParameter>(existingParams);

            // Define patterns for common parameter types
            var patterns = new Dictionary<string, Regex>
            {
                // Date pattern (various formats)
                { "DateTime", new Regex(@"\b(\d{1,2}[-/\.]\d{1,2}[-/\.]\d{2,4}|\d{4}[-/\.]\d{1,2}[-/\.]\d{1,2}|today|yesterday|tomorrow|(next|last) (week|month|year))\b", RegexOptions.IgnoreCase) },
                
                // Time pattern (HH:MM, with optional AM/PM)
                { "TimeSpan", new Regex(@"\b(\d{1,2}:\d{2}(:\d{2})?(\s*[AP]M)?)\b", RegexOptions.IgnoreCase) },
                
                // GUID/UUID pattern
                { "Guid", new Regex(@"\b([0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12})\b", RegexOptions.IgnoreCase) },
                
                // Number pattern (integer, decimal)
                { "Int32", new Regex(@"\b(\d+)\b") },
                { "Double", new Regex(@"\b(\d+\.\d+)\b") },

                // Boolean pattern (true/false, yes/no)
                { "Boolean", new Regex(@"\b(true|false|yes|no)\b", RegexOptions.IgnoreCase) }
            };

            // Look for each parameter that's still missing
            foreach (var param in template.Parameters.Where(p => !result.ContainsKey(p.Name)))
            {
                // If parameter is system parameter, skip it
                if (param.IsSystemParameter)
                {
                    continue;
                }

                // Look for presence of parameter name in query
                var paramNameRegex = new Regex($@"\b({param.Name}|{SplitCamelCase(param.Name)})\s*[:=]?\s*([^.,;]+)", RegexOptions.IgnoreCase);
                var paramMatch = paramNameRegex.Match(query);
                
                if (paramMatch.Success)
                {
                    var extractedValue = paramMatch.Groups[2].Value.Trim();
                    var convertedValue = ConvertToType(extractedValue, param.Type);
                    
                    if (convertedValue != null)
                    {
                        result[param.Name] = new ExtractedParameter
                        {
                            Name = param.Name,
                            Value = convertedValue,
                            Confidence = 0.8, // High confidence since parameter name is mentioned
                            OriginalText = extractedValue
                        };
                        continue;
                    }
                }

                // If we don't have a specific pattern for this type, continue
                if (!patterns.TryGetValue(param.Type, out var pattern))
                {
                    continue;
                }

                // Look for pattern match
                var match = pattern.Match(query);
                if (match.Success)
                {
                    var extractedValue = match.Groups[1].Value;
                    var convertedValue = ConvertToType(extractedValue, param.Type);
                    
                    if (convertedValue != null)
                    {
                        result[param.Name] = new ExtractedParameter
                        {
                            Name = param.Name,
                            Value = convertedValue,
                            Confidence = 0.6, // Medium confidence since we're matching patterns without explicit parameter names
                            OriginalText = extractedValue
                        };
                    }
                }
            }

            return Task.FromResult(result);
        }

        private Task<Dictionary<string, ExtractedParameter>> TryPatternMatchingExtractionAsync(
            string query,
            SqlTemplate template,
            Dictionary<string, ExtractedParameter> existingParams,
            CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, ExtractedParameter>(existingParams);

            // Define type-specific extraction patterns based on common linguistic patterns
            var extractionPatterns = new Dictionary<string, List<string>>
            {
                // For string parameters (looking for quoted text or specific text mentions)
                { "String", new List<string> {
                    @"(?:find|search|get|with|where|containing)\s+(?:the\s+)?(?:name|text|string|value)\s+(?:is\s+|of\s+|like\s+|contains\s+)?['""]([^'""]+)['""]",
                    @"(?:find|search|get|with|where|containing)\s+(?:the\s+)?(?:name|text|string|value)\s+(?:is|of|like|contains)\s+(\w+)",
                    @"['""]([^'""]+)['""]", // Fallback to just quoted text
                } },
                
                // For number parameters (looking for quantities, counts, IDs)
                { "Int32", new List<string> {
                    @"(?:id|number|count|quantity|limit|top)\s+(?:is\s+|of\s+|=\s+)?(\d+)",
                    @"(\d+)\s+(?:items|records|rows|results)",
                } },
                
                // For date parameters
                { "DateTime", new List<string> {
                    @"(?:date|time|when)\s+(?:is\s+|of\s+|=\s+|from\s+|after\s+|before\s+|on\s+)?(\d{1,2}[-/\.]\d{1,2}[-/\.]\d{2,4})",
                    @"(?:date|time|when)\s+(?:is\s+|of\s+|=\s+|from\s+|after\s+|before\s+|on\s+)?(\d{4}[-/\.]\d{1,2}[-/\.]\d{1,2})",
                    @"(?:on|at|after|before|since|from)\s+(\d{1,2}[-/\.]\d{1,2}[-/\.]\d{2,4}|\d{4}[-/\.]\d{1,2}[-/\.]\d{1,2})",
                } },
                
                // For boolean parameters
                { "Boolean", new List<string> {
                    @"(?:is|are|has|have|should|must|active|enabled|visible)\s+(true|false|yes|no|active|inactive|enabled|disabled)",
                } }
            };

            // Map parameter types to these generic types for pattern matching
            var typeMap = new Dictionary<string, string>
            {
                { "String", "String" },
                { "Int32", "Int32" },
                { "Int64", "Int32" },
                { "Double", "Int32" },
                { "Decimal", "Int32" },
                { "Float", "Int32" },
                { "DateTime", "DateTime" },
                { "DateTimeOffset", "DateTime" },
                { "Date", "DateTime" },
                { "Boolean", "Boolean" },
                { "Bool", "Boolean" }
            };

            // Look for each parameter that's still missing
            foreach (var param in template.Parameters.Where(p => !result.ContainsKey(p.Name) && !p.IsSystemParameter))
            {
                // Map the parameter type to our generic types
                if (!typeMap.TryGetValue(param.Type, out var genericType))
                {
                    continue;
                }

                // If we don't have patterns for this type, continue
                if (!extractionPatterns.TryGetValue(genericType, out var patterns))
                {
                    continue;
                }

                // Try each pattern
                foreach (var patternStr in patterns)
                {
                    var pattern = new Regex(patternStr, RegexOptions.IgnoreCase);
                    var match = pattern.Match(query);
                    
                    if (match.Success)
                    {
                        var extractedValue = match.Groups[1].Value.Trim();
                        var convertedValue = ConvertToType(extractedValue, param.Type);
                        
                        if (convertedValue != null)
                        {
                            result[param.Name] = new ExtractedParameter
                            {
                                Name = param.Name,
                                Value = convertedValue,
                                Confidence = 0.7, // Medium-high confidence since we're using linguistic patterns
                                OriginalText = extractedValue
                            };
                            break;
                        }
                    }
                }
            }

            return Task.FromResult(result);
        }

        private async Task<Dictionary<string, ExtractedParameter>> TryLlmExtractionAsync(
            string query,
            SqlTemplate template,
            Dictionary<string, ExtractedParameter> existingParams,
            CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, ExtractedParameter>(existingParams);
            
            if (_ollamaClient == null)
            {
                return result;
            }

            try
            {
                // Check which parameters we still need to extract
                var missingParams = template.Parameters
                    .Where(p => !p.IsSystemParameter && !result.ContainsKey(p.Name))
                    .ToList();

                if (!missingParams.Any())
                {
                    return result;
                }

                // Build a prompt for the LLM to extract parameters
                var prompt = BuildParameterExtractionPrompt(query, missingParams);
                
                // Call the LLM
                var llmResponse = await _ollamaClient.GenerateCompletionAsync(prompt, cancellationToken: cancellationToken);
                
                // Parse the response as JSON
                var extractedParams = ParseLlmParameterResponse(llmResponse.Response, missingParams);
                
                // Add the extracted parameters to the result
                foreach (var param in extractedParams)
                {
                    if (!result.ContainsKey(param.Key))
                    {
                        result.Add(param.Key, param.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting parameters using LLM");
            }

            return result;
        }

        private string BuildParameterExtractionPrompt(string query, List<SqlTemplateParameter> parameters)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("Extract the following parameters from the query:");
            sb.AppendLine();
            
            foreach (var param in parameters)
            {
                sb.AppendLine($"- {param.Name}: {param.Type} - {param.Description}");
            }
            
            sb.AppendLine();
            sb.AppendLine("Query:");
            sb.AppendLine(query);
            sb.AppendLine();
            sb.AppendLine("Format your response as a JSON object with parameter names as keys and parameter values as values.");
            sb.AppendLine("If a parameter cannot be extracted, use null.");
            sb.AppendLine("Example:");
            sb.AppendLine("{");
            
            for (int i = 0; i < parameters.Count; i++)
            {
                var param = parameters[i];
                string exampleValue = GetExampleValueForType(param.Type);
                sb.Append($"  \"{param.Name}\": {exampleValue}");
                
                if (i < parameters.Count - 1)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    sb.AppendLine();
                }
            }
            
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        private Dictionary<string, ExtractedParameter> ParseLlmParameterResponse(string response, List<SqlTemplateParameter> parameters)
        {
            var result = new Dictionary<string, ExtractedParameter>();
            
            try
            {
                // Find the JSON object in the response
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');
                
                if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
                {
                    throw new InvalidOperationException("Could not find valid JSON in LLM response");
                }
                
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var responseObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                
                if (responseObj == null)
                {
                    throw new InvalidOperationException("Failed to deserialize LLM response");
                }
                
                foreach (var param in parameters)
                {
                    if (responseObj.TryGetValue(param.Name, out var extractedValue) && extractedValue != null)
                    {
                        var valueString = extractedValue.ToString();
                        var convertedValue = ConvertToType(valueString, param.Type);
                        
                        if (convertedValue != null)
                        {
                            result[param.Name] = new ExtractedParameter
                            {
                                Name = param.Name,
                                Value = convertedValue,
                                Confidence = 0.85, // High confidence since LLM is specifically designed for extraction
                                OriginalText = valueString
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing LLM parameter response");
            }
            
            return result;
        }

        private Dictionary<string, Func<string, object?>> InitializeTypeConverters()
        {
            return new Dictionary<string, Func<string, object?>>
            {
                { "String", (value) => value },
                { "Int32", (value) => Int32.TryParse(value, out var result) ? result : null },
                { "Int64", (value) => Int64.TryParse(value, out var result) ? result : null },
                { "Double", (value) => Double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null },
                { "Decimal", (value) => Decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null },
                { "Float", (value) => Single.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null },
                { "Boolean", (value) => 
                    {
                        if (Boolean.TryParse(value, out var result)) return result;
                        if (value.Equals("yes", StringComparison.OrdinalIgnoreCase) || 
                            value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                            value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                            value.Equals("active", StringComparison.OrdinalIgnoreCase) ||
                            value.Equals("enabled", StringComparison.OrdinalIgnoreCase))
                            return true;
                        if (value.Equals("no", StringComparison.OrdinalIgnoreCase) || 
                            value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                            value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                            value.Equals("inactive", StringComparison.OrdinalIgnoreCase) ||
                            value.Equals("disabled", StringComparison.OrdinalIgnoreCase))
                            return false;
                        return null;
                    }
                },
                { "DateTime", (value) => 
                    {
                        if (DateTime.TryParse(value, out var result)) return result;
                        
                        // Handle relative dates
                        if (value.Equals("today", StringComparison.OrdinalIgnoreCase))
                            return DateTime.Today;
                        if (value.Equals("yesterday", StringComparison.OrdinalIgnoreCase))
                            return DateTime.Today.AddDays(-1);
                        if (value.Equals("tomorrow", StringComparison.OrdinalIgnoreCase))
                            return DateTime.Today.AddDays(1);
                        if (value.Equals("next week", StringComparison.OrdinalIgnoreCase))
                            return DateTime.Today.AddDays(7);
                        if (value.Equals("last week", StringComparison.OrdinalIgnoreCase))
                            return DateTime.Today.AddDays(-7);
                        if (value.Equals("next month", StringComparison.OrdinalIgnoreCase))
                            return DateTime.Today.AddMonths(1);
                        if (value.Equals("last month", StringComparison.OrdinalIgnoreCase))
                            return DateTime.Today.AddMonths(-1);
                        if (value.Equals("next year", StringComparison.OrdinalIgnoreCase))
                            return DateTime.Today.AddYears(1);
                        if (value.Equals("last year", StringComparison.OrdinalIgnoreCase))
                            return DateTime.Today.AddYears(-1);
                            
                        return null;
                    }
                },
                { "DateTimeOffset", (value) => 
                    {
                        if (DateTimeOffset.TryParse(value, out var result)) return result;
                        
                        // Try parsing as DateTime first, then convert
                        var dateTime = ConvertToType(value, "DateTime") as DateTime?;
                        if (dateTime.HasValue)
                            return new DateTimeOffset(dateTime.Value);
                            
                        return null; 
                    }
                },
                { "TimeSpan", (value) => TimeSpan.TryParse(value, out var result) ? result : null },
                { "Guid", (value) => Guid.TryParse(value, out var result) ? result : null }
            };
        }

        private object? ConvertToType(string? value, string type)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (_typeConverters.TryGetValue(type, out var converter))
            {
                return converter(value);
            }

            // Default to string for unknown types
            return value;
        }

        private bool IsTypeCompatible(object value, string type)
        {
            // Simple compatibility check based on runtime type
            switch (type)
            {
                case "String":
                    return true; // All values can be strings
                case "Int32":
                    return value is int;
                case "Int64":
                    return value is long || value is int;
                case "Double":
                    return value is double || value is float || value is int;
                case "Decimal":
                    return value is decimal || value is double || value is float || value is int;
                case "Float":
                    return value is float || value is int;
                case "Boolean":
                    return value is bool;
                case "DateTime":
                    return value is DateTime;
                case "DateTimeOffset":
                    return value is DateTimeOffset || value is DateTime;
                case "TimeSpan":
                    return value is TimeSpan;
                case "Guid":
                    return value is Guid;
                default:
                    return true; // For unknown types, assume compatibility
            }
        }

        private string SplitCamelCase(string input)
        {
            return Regex.Replace(input, "([a-z])([A-Z])", "$1 $2").ToLower();
        }

        private string GetExampleValueForType(string type)
        {
            switch (type)
            {
                case "String":
                    return "\"example\"";
                case "Int32":
                case "Int64":
                    return "42";
                case "Double":
                case "Decimal":
                case "Float":
                    return "3.14";
                case "Boolean":
                    return "true";
                case "DateTime":
                case "DateTimeOffset":
                    return "\"2023-06-15\"";
                case "TimeSpan":
                    return "\"12:30:00\"";
                case "Guid":
                    return "\"00000000-0000-0000-0000-000000000000\"";
                default:
                    return "null";
            }
        }

        #endregion
    }
} 