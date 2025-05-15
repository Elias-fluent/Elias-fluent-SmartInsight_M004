using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.SQL.Models;

namespace SmartInsight.AI.SQL.Interfaces
{
    /// <summary>
    /// Interface for SQL template storage operations
    /// </summary>
    public interface ITemplateRepository
    {
        /// <summary>
        /// Gets a template by its ID
        /// </summary>
        /// <param name="templateId">The template ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The template, or null if not found</returns>
        Task<SqlTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all templates
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all templates</returns>
        Task<List<SqlTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds templates by matching intent
        /// </summary>
        /// <param name="intent">The intent to match</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of templates that match the intent</returns>
        Task<List<SqlTemplate>> FindTemplatesByIntentAsync(string intent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new template or updates an existing one
        /// </summary>
        /// <param name="template">The template to add or update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveTemplateAsync(SqlTemplate template, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a template
        /// </summary>
        /// <param name="templateId">The ID of the template to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds templates by tags
        /// </summary>
        /// <param name="tags">List of tags to search for</param>
        /// <param name="matchAll">If true, all tags must match; if false, any tag can match</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of templates that match the tags</returns>
        Task<List<SqlTemplate>> FindTemplatesByTagsAsync(
            List<string> tags, 
            bool matchAll = false, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current version of a template
        /// </summary>
        /// <param name="templateId">The template ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The current version string, or null if template not found</returns>
        Task<string?> GetTemplateVersionAsync(string templateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a template for correctness
        /// </summary>
        /// <param name="template">The template to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> ValidateTemplateAsync(SqlTemplate template, CancellationToken cancellationToken = default);
    }
} 