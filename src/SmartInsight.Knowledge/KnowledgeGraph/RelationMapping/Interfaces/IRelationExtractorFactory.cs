using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.RelationMapping.Interfaces
{
    /// <summary>
    /// Interface for a factory that creates relation extractors
    /// </summary>
    public interface IRelationExtractorFactory
    {
        /// <summary>
        /// Creates a specific relation extractor by type name
        /// </summary>
        /// <param name="typeName">The relation extractor type name</param>
        /// <returns>The relation extractor instance, or null if not supported</returns>
        IRelationExtractor CreateExtractor(string typeName);
        
        /// <summary>
        /// Creates all supported relation extractors
        /// </summary>
        /// <returns>A collection of relation extractors</returns>
        IEnumerable<IRelationExtractor> CreateAllExtractors();
        
        /// <summary>
        /// Gets the names of all supported relation extractor types
        /// </summary>
        /// <returns>The supported extractor type names</returns>
        IEnumerable<string> GetSupportedExtractorTypes();
    }
} 