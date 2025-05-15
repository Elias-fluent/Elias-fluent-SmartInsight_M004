using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartInsight.AI.Models
{
    /// <summary>
    /// Represents a conversation context object that maintains history and state across interactions.
    /// </summary>
    public class ConversationContext
    {
        /// <summary>
        /// Unique identifier for the conversation.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The user identifier associated with this conversation.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// The timestamp when the conversation was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The timestamp when the conversation was last updated.
        /// </summary>
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Messages in the conversation, ordered by timestamp.
        /// </summary>
        public List<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();

        /// <summary>
        /// Entities that have been identified and tracked across the conversation.
        /// </summary>
        public List<TrackedEntity> TrackedEntities { get; set; } = new List<TrackedEntity>();

        /// <summary>
        /// A record of intents detected throughout the conversation.
        /// </summary>
        public List<DetectedIntent> DetectedIntents { get; set; } = new List<DetectedIntent>();

        /// <summary>
        /// Key-value pairs for storing additional state information.
        /// </summary>
        public Dictionary<string, string> StateData { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The most recent query in this conversation.
        /// </summary>
        public string CurrentQuery { get; set; } = string.Empty;

        /// <summary>
        /// Adds a new message to the conversation.
        /// </summary>
        /// <param name="role">The role of the message sender (user, assistant, system).</param>
        /// <param name="content">The content of the message.</param>
        /// <returns>The added message.</returns>
        public ConversationMessage AddMessage(string role, string content)
        {
            var message = new ConversationMessage
            {
                Role = role,
                Content = content,
                Timestamp = DateTime.UtcNow
            };
            
            Messages.Add(message);
            LastUpdatedAt = DateTime.UtcNow;
            
            return message;
        }

        /// <summary>
        /// Adds a tracked entity to the conversation context.
        /// </summary>
        /// <param name="entity">The entity to track.</param>
        public void AddTrackedEntity(TrackedEntity entity)
        {
            // Check if entity already exists
            var existingEntity = TrackedEntities.FirstOrDefault(e => 
                e.Entity.Type == entity.Entity.Type && 
                e.Entity.Value == entity.Entity.Value);
            
            if (existingEntity != null)
            {
                // Update existing entity
                existingEntity.LastMentionedAt = DateTime.UtcNow;
                existingEntity.MentionCount++;
                existingEntity.Entity.Confidence = Math.Max(existingEntity.Entity.Confidence, entity.Entity.Confidence);
            }
            else
            {
                // Add new entity
                TrackedEntities.Add(entity);
            }
            
            LastUpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Records a detected intent in the conversation.
        /// </summary>
        /// <param name="intent">The detected intent to record.</param>
        public void AddDetectedIntent(DetectedIntent intent)
        {
            DetectedIntents.Add(intent);
            LastUpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the relevant conversation history within a specified window size.
        /// </summary>
        /// <param name="maxMessages">Maximum number of messages to include.</param>
        /// <returns>A list of relevant conversation messages.</returns>
        public List<ConversationMessage> GetRelevantHistory(int maxMessages = 10)
        {
            // If we have fewer messages than the max, return all
            if (Messages.Count <= maxMessages)
            {
                return Messages.ToList();
            }
            
            // Otherwise, select the most recent messages up to maxMessages
            return Messages.OrderByDescending(m => m.Timestamp)
                .Take(maxMessages)
                .OrderBy(m => m.Timestamp) // Re-order chronologically
                .ToList();
        }

        /// <summary>
        /// Gets a summary of the conversation context.
        /// </summary>
        /// <returns>A string summarizing the conversation context.</returns>
        public string GetContextSummary()
        {
            var summary = new System.Text.StringBuilder();
            
            // Add information about tracked entities
            if (TrackedEntities.Any())
            {
                summary.AppendLine("Key entities in this conversation:");
                foreach (var entity in TrackedEntities.OrderByDescending(e => e.MentionCount).Take(5))
                {
                    summary.AppendLine($"- {entity.Entity.Type}: {entity.Entity.Value}");
                }
                summary.AppendLine();
            }
            
            // Add information about recent intents
            if (DetectedIntents.Any())
            {
                summary.AppendLine("Recent topics discussed:");
                foreach (var intent in DetectedIntents.OrderByDescending(i => i.DetectedAt).Take(3))
                {
                    summary.AppendLine($"- {intent.Intent}");
                }
                summary.AppendLine();
            }
            
            return summary.ToString();
        }
    }

    /// <summary>
    /// Represents an entity that is tracked across multiple conversation turns.
    /// </summary>
    public class TrackedEntity
    {
        /// <summary>
        /// The entity being tracked.
        /// </summary>
        public Entity Entity { get; set; } = null!;

        /// <summary>
        /// When the entity was first mentioned in the conversation.
        /// </summary>
        public DateTime FirstMentionedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the entity was last mentioned in the conversation.
        /// </summary>
        public DateTime LastMentionedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of times the entity has been mentioned.
        /// </summary>
        public int MentionCount { get; set; } = 1;

        /// <summary>
        /// The relevance score of the entity to the overall conversation (0.0 to 1.0).
        /// </summary>
        public double RelevanceScore { get; set; } = 0.5;

        /// <summary>
        /// Whether this entity is a key entity in the conversation.
        /// </summary>
        public bool IsKeyEntity => MentionCount > 1 || RelevanceScore > 0.7;
    }

    /// <summary>
    /// Represents an intent that was detected in the conversation.
    /// </summary>
    public class DetectedIntent
    {
        /// <summary>
        /// The name of the detected intent.
        /// </summary>
        public string Intent { get; set; } = string.Empty;

        /// <summary>
        /// The confidence score of the intent detection.
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// When the intent was detected.
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The query that triggered this intent.
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Entities associated with this intent detection.
        /// </summary>
        public List<Entity> Entities { get; set; } = new List<Entity>();
    }

    /// <summary>
    /// Options for configuring context management behavior.
    /// </summary>
    public class ContextManagementOptions
    {
        /// <summary>
        /// Maximum number of messages to keep in history.
        /// </summary>
        public int MaxHistoryMessages { get; set; } = 20;

        /// <summary>
        /// Maximum number of tracked entities to maintain.
        /// </summary>
        public int MaxTrackedEntities { get; set; } = 50;

        /// <summary>
        /// Maximum number of detected intents to store.
        /// </summary>
        public int MaxDetectedIntents { get; set; } = 10;

        /// <summary>
        /// Time in hours after which tracked entities become less relevant.
        /// </summary>
        public double EntityDecayTimeHours { get; set; } = 1.0;

        /// <summary>
        /// Whether to generate summaries for long conversations.
        /// </summary>
        public bool EnableSummarization { get; set; } = true;

        /// <summary>
        /// Number of messages after which to trigger summarization.
        /// </summary>
        public int SummarizationThreshold { get; set; } = 10;
    }
} 