using System.Collections.Generic;

namespace SmartInsight.Knowledge.VectorDb.Embeddings
{
    /// <summary>
    /// Interface for chunking text into appropriately sized segments for embedding
    /// </summary>
    public interface ITextChunker
    {
        /// <summary>
        /// Splits text into optimal sized chunks for embedding generation
        /// </summary>
        /// <param name="text">The text to chunk</param>
        /// <param name="maxChunkSize">Maximum characters per chunk (default: 1000)</param>
        /// <param name="overlap">Number of characters to overlap between chunks (default: 200)</param>
        /// <returns>List of text chunks</returns>
        List<TextChunk> ChunkText(string text, int maxChunkSize = 1000, int overlap = 200);
        
        /// <summary>
        /// Splits a document into chunks considering document structure
        /// </summary>
        /// <param name="documentText">The document text</param>
        /// <param name="documentTitle">Optional document title</param>
        /// <param name="maxChunkSize">Maximum characters per chunk (default: 1000)</param>
        /// <param name="overlap">Number of characters to overlap between chunks (default: 200)</param>
        /// <returns>List of text chunks with metadata</returns>
        List<TextChunk> ChunkDocument(string documentText, string? documentTitle = null, int maxChunkSize = 1000, int overlap = 200);
    }
    
    /// <summary>
    /// Represents a chunk of text with metadata
    /// </summary>
    public class TextChunk
    {
        /// <summary>
        /// The text content of the chunk
        /// </summary>
        public string Text { get; set; } = string.Empty;
        
        /// <summary>
        /// Metadata about the chunk and its source
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Sequential position within the document or source
        /// </summary>
        public int Position { get; set; }
        
        /// <summary>
        /// Source document ID if applicable
        /// </summary>
        public string? SourceId { get; set; }
        
        /// <summary>
        /// Document section or heading if applicable
        /// </summary>
        public string? Section { get; set; }
    }
} 