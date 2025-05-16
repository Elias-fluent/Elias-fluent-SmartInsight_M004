using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SmartInsight.Knowledge.VectorDb.Embeddings
{
    /// <summary>
    /// Implementation of text chunking logic for embeddings
    /// </summary>
    public class TextChunker : ITextChunker
    {
        private readonly ILogger<TextChunker> _logger;
        private readonly EmbeddingOptions _options;
        
        // Regex for finding paragraph breaks
        private static readonly Regex _paragraphBreakRegex = new Regex(@"\n\s*\n", RegexOptions.Compiled);
        
        // Regex for finding sentence breaks
        private static readonly Regex _sentenceBreakRegex = new Regex(@"(?<=[.!?])\s+(?=[A-Z])", RegexOptions.Compiled);
        
        // Regex for finding headers
        private static readonly Regex _headerRegex = new Regex(@"^\s*#{1,6}\s+(.+)$|^(.+)\n[=\-]{2,}$", RegexOptions.Compiled | RegexOptions.Multiline);
        
        public TextChunker(
            ILogger<TextChunker> logger,
            IOptions<EmbeddingOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public List<TextChunk> ChunkText(string text, int maxChunkSize = 1000, int overlap = 200)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<TextChunk>();
            }
            
            // Use default values from options if not specified
            if (maxChunkSize <= 0)
            {
                maxChunkSize = _options.DefaultChunkSize;
            }
            
            if (overlap <= 0)
            {
                overlap = _options.DefaultChunkOverlap;
            }
            
            // Ensure overlap is less than max chunk size
            overlap = Math.Min(overlap, maxChunkSize / 2);
            
            _logger.LogDebug("Chunking text with max size {MaxSize} and overlap {Overlap}", 
                maxChunkSize, overlap);
            
            var chunks = new List<TextChunk>();
            
            // For very short text, just create a single chunk
            if (text.Length <= maxChunkSize)
            {
                chunks.Add(new TextChunk
                {
                    Text = text,
                    Position = 0
                });
                
                return chunks;
            }
            
            // Split text by paragraph breaks
            var paragraphs = _paragraphBreakRegex.Split(text)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
            
            var currentChunk = new List<string>();
            var currentLength = 0;
            var position = 0;
            
            foreach (var paragraph in paragraphs)
            {
                // If paragraph is longer than max size, split it by sentences
                if (paragraph.Length > maxChunkSize)
                {
                    var sentences = SplitBySentences(paragraph);
                    foreach (var sentence in sentences)
                    {
                        // If sentence is longer than max size, split it by character
                        if (sentence.Length > maxChunkSize)
                        {
                            var sentenceChunks = SplitBySize(sentence, maxChunkSize, overlap);
                            foreach (var sentenceChunk in sentenceChunks)
                            {
                                chunks.Add(new TextChunk
                                {
                                    Text = sentenceChunk,
                                    Position = position++
                                });
                            }
                        }
                        else if (currentLength + sentence.Length > maxChunkSize)
                        {
                            // Current chunk would exceed max size, save it and start a new one
                            if (currentChunk.Count > 0)
                            {
                                var chunkText = string.Join(" ", currentChunk);
                                chunks.Add(new TextChunk
                                {
                                    Text = chunkText,
                                    Position = position++
                                });
                            }
                            
                            currentChunk.Clear();
                            currentChunk.Add(sentence);
                            currentLength = sentence.Length;
                        }
                        else
                        {
                            // Add sentence to current chunk
                            currentChunk.Add(sentence);
                            currentLength += sentence.Length;
                        }
                    }
                }
                else if (currentLength + paragraph.Length > maxChunkSize)
                {
                    // Current chunk would exceed max size, save it and start a new one
                    if (currentChunk.Count > 0)
                    {
                        var chunkText = string.Join(" ", currentChunk);
                        chunks.Add(new TextChunk
                        {
                            Text = chunkText,
                            Position = position++
                        });
                    }
                    
                    currentChunk.Clear();
                    currentChunk.Add(paragraph);
                    currentLength = paragraph.Length;
                }
                else
                {
                    // Add paragraph to current chunk
                    currentChunk.Add(paragraph);
                    currentLength += paragraph.Length;
                }
            }
            
            // Add the last chunk if there's anything left
            if (currentChunk.Count > 0)
            {
                var chunkText = string.Join(" ", currentChunk);
                chunks.Add(new TextChunk
                {
                    Text = chunkText,
                    Position = position
                });
            }
            
            _logger.LogDebug("Text chunked into {ChunkCount} chunks", chunks.Count);
            
            return chunks;
        }

        /// <inheritdoc />
        public List<TextChunk> ChunkDocument(string documentText, string? documentTitle = null, int maxChunkSize = 1000, int overlap = 200)
        {
            if (string.IsNullOrWhiteSpace(documentText))
            {
                return new List<TextChunk>();
            }
            
            // Use default values from options if not specified
            if (maxChunkSize <= 0)
            {
                maxChunkSize = _options.DefaultChunkSize;
            }
            
            if (overlap <= 0)
            {
                overlap = _options.DefaultChunkOverlap;
            }
            
            _logger.LogDebug("Chunking document with max size {MaxSize} and overlap {Overlap}", 
                maxChunkSize, overlap);
            
            var chunks = new List<TextChunk>();
            var currentHeader = documentTitle ?? "Document";
            
            // Find all headers in the document
            var headers = _headerRegex.Matches(documentText)
                .Cast<Match>()
                .Select(m => new {
                    Match = m,
                    Text = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value,
                    Start = m.Index,
                    End = m.Index + m.Length
                })
                .OrderBy(h => h.Start)
                .ToList();
            
            // If no headers found, just chunk the text
            if (headers.Count == 0)
            {
                var textChunks = ChunkText(documentText, maxChunkSize, overlap);
                
                // Add document title as metadata
                foreach (var chunk in textChunks)
                {
                    chunk.Section = currentHeader;
                    chunk.Metadata["title"] = documentTitle ?? "Document";
                }
                
                return textChunks;
            }
            
            // Process each section defined by headers
            int currentIndex = 0;
            foreach (var header in headers)
            {
                // Process text before this header if we're not at the start
                if (header.Start > currentIndex)
                {
                    var sectionText = documentText.Substring(currentIndex, header.Start - currentIndex);
                    if (!string.IsNullOrWhiteSpace(sectionText))
                    {
                        var sectionChunks = ChunkText(sectionText, maxChunkSize, overlap);
                        foreach (var chunk in sectionChunks)
                        {
                            chunk.Section = currentHeader;
                            chunk.Metadata["title"] = documentTitle ?? "Document";
                            chunks.Add(chunk);
                        }
                    }
                }
                
                // Update current header
                currentHeader = header.Text.Trim();
                // Move index past the header
                currentIndex = header.End;
            }
            
            // Process remaining text
            if (currentIndex < documentText.Length)
            {
                var sectionText = documentText.Substring(currentIndex);
                if (!string.IsNullOrWhiteSpace(sectionText))
                {
                    var sectionChunks = ChunkText(sectionText, maxChunkSize, overlap);
                    foreach (var chunk in sectionChunks)
                    {
                        chunk.Section = currentHeader;
                        chunk.Metadata["title"] = documentTitle ?? "Document";
                        chunks.Add(chunk);
                    }
                }
            }
            
            // Update positions to be sequential across the entire document
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].Position = i;
            }
            
            _logger.LogDebug("Document chunked into {ChunkCount} chunks with {HeaderCount} sections", 
                chunks.Count, headers.Count);
            
            return chunks;
        }
        
        /// <summary>
        /// Splits text by sentences
        /// </summary>
        private List<string> SplitBySentences(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }
            
            return _sentenceBreakRegex.Split(text)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }
        
        /// <summary>
        /// Splits text by fixed size with overlap
        /// </summary>
        private List<string> SplitBySize(string text, int maxSize, int overlap)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }
            
            if (text.Length <= maxSize)
            {
                return new List<string> { text };
            }
            
            var chunks = new List<string>();
            int chunkStart = 0;
            
            while (chunkStart < text.Length)
            {
                int chunkEnd = Math.Min(chunkStart + maxSize, text.Length);
                chunks.Add(text.Substring(chunkStart, chunkEnd - chunkStart));
                chunkStart = chunkEnd - overlap;
                
                // If we can't advance further, break to avoid infinite loop
                if (chunkStart + overlap >= text.Length)
                {
                    break;
                }
            }
            
            return chunks;
        }
    }
} 