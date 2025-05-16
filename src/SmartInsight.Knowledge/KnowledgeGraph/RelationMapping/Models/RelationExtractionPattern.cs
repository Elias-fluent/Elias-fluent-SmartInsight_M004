using System.Collections.Generic;
using System.Text.RegularExpressions;
using SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Models;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Models
{
    /// <summary>
    /// Represents a pattern for extracting relations between entities
    /// </summary>
    public class RelationExtractionPattern
    {
        /// <summary>
        /// Unique identifier for the pattern
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Human-readable name for the pattern
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The relation type this pattern extracts
        /// </summary>
        public RelationType RelationType { get; set; }
        
        /// <summary>
        /// The regular expression pattern to match in text
        /// </summary>
        public string RegexPattern { get; set; }
        
        /// <summary>
        /// Compiled regex for performance
        /// </summary>
        public Regex CompiledRegex { get; set; }
        
        /// <summary>
        /// The source entity type for this relation
        /// </summary>
        public EntityType SourceEntityType { get; set; }
        
        /// <summary>
        /// The target entity type for this relation
        /// </summary>
        public EntityType TargetEntityType { get; set; }
        
        /// <summary>
        /// If true, the pattern matches in both directions
        /// </summary>
        public bool IsBidirectional { get; set; }
        
        /// <summary>
        /// Maximum token distance between source and target entities
        /// </summary>
        public int MaxTokenDistance { get; set; } = 10;
        
        /// <summary>
        /// Base confidence score for relations extracted with this pattern
        /// </summary>
        public double BaseConfidenceScore { get; set; } = 0.7;
        
        /// <summary>
        /// List of trigger words or phrases that indicate this relation
        /// </summary>
        public List<string> TriggerPhrases { get; set; } = new List<string>();
        
        /// <summary>
        /// Creates a relation extraction pattern
        /// </summary>
        public RelationExtractionPattern()
        {
        }
        
        /// <summary>
        /// Creates a relation extraction pattern with a compiled regex
        /// </summary>
        /// <param name="regexPattern">The regex pattern string</param>
        public RelationExtractionPattern(string regexPattern)
        {
            RegexPattern = regexPattern;
            CompiledRegex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
} 