using System.Collections.Generic;

namespace SmartInsight.Knowledge.KnowledgeGraph.EntityExtraction.Interfaces
{
    /// <summary>
    /// Interface for a factory that creates entity extractors
    /// </summary>
    public interface IEntityExtractorFactory
    {
        /// <summary>
        /// Creates an entity extractor by type name
        /// </summary>
        /// <param name="extractorType">The type name of the extractor to create</param>
        /// <returns>The created entity extractor</returns>
        IEntityExtractor CreateExtractor(string extractorType);
        
        /// <summary>
        /// Creates all registered entity extractors
        /// </summary>
        /// <returns>A collection of all available entity extractors</returns>
        IEnumerable<IEntityExtractor> CreateAllExtractors();
        
        /// <summary>
        /// Gets all available extractor type names
        /// </summary>
        /// <returns>A collection of available extractor type names</returns>
        IEnumerable<string> GetAvailableExtractorTypes();
    }
} 