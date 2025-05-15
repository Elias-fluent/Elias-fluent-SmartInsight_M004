using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartInsight.AI.Intent;
using SmartInsight.AI.Models;

namespace SmartInsight.AI.Interfaces
{
    /// <summary>
    /// Interface for intent classification services using vector embeddings
    /// </summary>
    public interface IIntentClassifier
    {
        /// <summary>
        /// Get the current intent classification model
        /// </summary>
        /// <returns>The current model</returns>
        IntentClassificationModel GetModel();
        
        /// <summary>
        /// Set a new intent classification model
        /// </summary>
        /// <param name="model">The model to set</param>
        void SetModel(IntentClassificationModel model);
        
        /// <summary>
        /// Add a new intent with examples and generate embeddings
        /// </summary>
        /// <param name="name">Intent name</param>
        /// <param name="description">Description of the intent</param>
        /// <param name="examples">Example queries for this intent</param>
        /// <param name="entitySlots">Optional entity slots for this intent</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created intent definition</returns>
        Task<IntentDefinition> AddIntentAsync(
            string name, 
            string description, 
            List<string> examples,
            List<EntitySlot>? entitySlots = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Classify a query against all known intents
        /// </summary>
        /// <param name="query">The query to classify</param>
        /// <param name="similarityThreshold">Optional override for similarity threshold</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The classification result</returns>
        Task<ClassificationResult> ClassifyAsync(
            string query, 
            double? similarityThreshold = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Classify a query with conversation context for enhanced confidence scoring
        /// </summary>
        /// <param name="query">The query to classify</param>
        /// <param name="conversationId">The conversation ID for context retrieval</param>
        /// <param name="similarityThreshold">Optional override for similarity threshold</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Classification result with context-aware confidence</returns>
        Task<ClassificationResult> ClassifyWithContextAsync(
            string query,
            string conversationId,
            double? similarityThreshold = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Update examples for an existing intent
        /// </summary>
        /// <param name="intentName">The intent name</param>
        /// <param name="examples">New examples</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful, false if intent not found</returns>
        Task<bool> UpdateIntentExamplesAsync(
            string intentName, 
            List<string> examples,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Remove an intent and its aliases
        /// </summary>
        /// <param name="intentName">The intent name</param>
        /// <returns>True if successful, false if intent not found</returns>
        bool RemoveIntent(string intentName);
    }
} 