using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Models;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for extracting SQL parameters from natural language
    /// </summary>
    public interface IParameterExtractor
    {
        /// <summary>
        /// Extracts parameters from a natural language query based on template requirements
        /// </summary>
        /// <param name="query">The natural language query</param>
        /// <param name="template">The SQL template with parameter definitions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of parameter names to their extracted values</returns>
        Task<Dictionary<string, ExtractedParameter>> ExtractParametersAsync(
            string query, 
            SqlTemplate template, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts parameters using intent detection results
        /// </summary>
        /// <param name="intentResult">The intent detection result with entities</param>
        /// <param name="template">The SQL template with parameter definitions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of parameter names to their extracted values</returns>
        Task<Dictionary<string, ExtractedParameter>> ExtractParametersAsync(
            IntentDetectionResult intentResult, 
            SqlTemplate template, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts parameters using reasoning results
        /// </summary>
        /// <param name="reasoningResult">The reasoning result with entities</param>
        /// <param name="template">The SQL template with parameter definitions</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary of parameter names to their extracted values</returns>
        Task<Dictionary<string, ExtractedParameter>> ExtractParametersAsync(
            ReasoningResult reasoningResult, 
            SqlTemplate template, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates extracted parameters against template requirements
        /// </summary>
        /// <param name="parameters">The extracted parameters</param>
        /// <param name="template">The SQL template with parameter definitions</param>
        /// <returns>Validation result with any missing or invalid parameters</returns>
        Task<ParameterValidationResult> ValidateParametersAsync(
            Dictionary<string, ExtractedParameter> parameters, 
            SqlTemplate template);

        /// <summary>
        /// Converts extracted parameters to proper SQL parameter types
        /// </summary>
        /// <param name="parameters">The extracted parameters</param>
        /// <param name="template">The SQL template with parameter definitions</param>
        /// <returns>Dictionary of parameter names to correctly typed values</returns>
        Task<Dictionary<string, object>> ConvertToSqlParametersAsync(
            Dictionary<string, ExtractedParameter> parameters, 
            SqlTemplate template);
    }

    /// <summary>
    /// Result of parameter validation
    /// </summary>
    public class ParameterValidationResult
    {
        /// <summary>
        /// Whether all required parameters are present and valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of missing required parameters
        /// </summary>
        public List<string> MissingParameters { get; set; } = new List<string>();

        /// <summary>
        /// List of parameters with invalid values
        /// </summary>
        public Dictionary<string, string> InvalidParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// List of parameters with low confidence
        /// </summary>
        public Dictionary<string, double> LowConfidenceParameters { get; set; } = new Dictionary<string, double>();
    }
} 