using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.TripleStore.Models
{
    /// <summary>
    /// Represents the differences between two versions of a triple
    /// </summary>
    public class TripleDiff
    {
        /// <summary>
        /// ID of the triple being compared
        /// </summary>
        public string TripleId { get; set; }
        
        /// <summary>
        /// From version number
        /// </summary>
        public int FromVersion { get; set; }
        
        /// <summary>
        /// To version number
        /// </summary>
        public int ToVersion { get; set; }
        
        /// <summary>
        /// The changes to the subject
        /// </summary>
        public PropertyChange<string> SubjectChange { get; set; }
        
        /// <summary>
        /// The changes to the predicate
        /// </summary>
        public PropertyChange<string> PredicateChange { get; set; }
        
        /// <summary>
        /// The changes to the object
        /// </summary>
        public PropertyChange<string> ObjectChange { get; set; }
        
        /// <summary>
        /// Changes to other properties
        /// </summary>
        public Dictionary<string, PropertyChange<object>> OtherChanges { get; set; } = new Dictionary<string, PropertyChange<object>>();
        
        /// <summary>
        /// Type of change between the versions
        /// </summary>
        public ChangeType ChangeType { get; set; }
        
        /// <summary>
        /// Whether this is a core semantic change (subject, predicate, or object changed)
        /// </summary>
        public bool IsCoreSemantic => 
            SubjectChange?.HasChanged == true || 
            PredicateChange?.HasChanged == true || 
            ObjectChange?.HasChanged == true;
        
        /// <summary>
        /// Human-readable summary of the changes
        /// </summary>
        public string GetChangeSummary()
        {
            var changes = new List<string>();
            
            if (SubjectChange?.HasChanged == true)
                changes.Add($"Subject changed from '{SubjectChange.OldValue}' to '{SubjectChange.NewValue}'");
                
            if (PredicateChange?.HasChanged == true)
                changes.Add($"Predicate changed from '{PredicateChange.OldValue}' to '{PredicateChange.NewValue}'");
                
            if (ObjectChange?.HasChanged == true)
                changes.Add($"Object changed from '{ObjectChange.OldValue}' to '{ObjectChange.NewValue}'");
                
            foreach (var change in OtherChanges)
            {
                if (change.Value.HasChanged)
                    changes.Add($"{change.Key} changed from '{change.Value.OldValue}' to '{change.Value.NewValue}'");
            }
            
            return string.Join("; ", changes);
        }
    }
    
    /// <summary>
    /// Represents a change to a property between two versions
    /// </summary>
    /// <typeparam name="T">The type of the property</typeparam>
    public class PropertyChange<T>
    {
        /// <summary>
        /// The value in the older version
        /// </summary>
        public T OldValue { get; set; }
        
        /// <summary>
        /// The value in the newer version
        /// </summary>
        public T NewValue { get; set; }
        
        /// <summary>
        /// Whether the value has changed
        /// </summary>
        public bool HasChanged => !EqualityComparer<T>.Default.Equals(OldValue, NewValue);
    }
} 