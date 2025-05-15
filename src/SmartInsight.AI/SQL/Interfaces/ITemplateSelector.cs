using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Models;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for selecting SQL templates based on natural language intent
    /// </summary>
    public interface ITemplateSelector
    {
        /// <summary>
        /// Selects a template based on intent detection result
        /// </summary>
        /// <param name="intentResult">The intent detection result</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The template selection result</returns>
        Task<TemplateSelectionResult> SelectTemplateAsync(
            IntentDetectionResult intentResult, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Selects a template based on reasoning result
        /// </summary>
        /// <param name="reasoningResult">The reasoning result</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The template selection result</returns>
        Task<TemplateSelectionResult> SelectTemplateAsync(
            ReasoningResult reasoningResult, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Selects a template based on hierarchical intent result
        /// </summary>
        /// <param name="hierarchicalIntent">The hierarchical intent result</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The template selection result</returns>
        Task<TemplateSelectionResult> SelectTemplateAsync(
            HierarchicalIntentResult hierarchicalIntent, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Selects a template based on natural language query without pre-determined intent
        /// </summary>
        /// <param name="query">The natural language query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The template selection result</returns>
        Task<TemplateSelectionResult> SelectTemplateFromQueryAsync(
            string query, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the minimum confidence threshold for template selection
        /// </summary>
        /// <param name="threshold">The confidence threshold (0.0 to 1.0)</param>
        void SetConfidenceThreshold(double threshold);
    }
} 