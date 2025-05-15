namespace SmartInsight.Core.Interfaces;

/// <summary>
/// Interface for AI model operations
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Generates a response to a prompt
    /// </summary>
    /// <param name="prompt">The input prompt</param>
    /// <param name="systemPrompt">Optional system prompt for context</param>
    /// <param name="conversationHistory">Optional conversation history</param>
    /// <param name="maxTokens">Maximum tokens to generate</param>
    /// <param name="temperature">Temperature for sampling (0.0 to 1.0)</param>
    /// <returns>Generated text response</returns>
    Task<string> GenerateTextAsync(
        string prompt,
        string? systemPrompt = null,
        IList<(string Role, string Content)>? conversationHistory = null,
        int maxTokens = 1024,
        float temperature = 0.7f);
    
    /// <summary>
    /// Generates a text embedding vector for the given text
    /// </summary>
    /// <param name="text">Input text to embed</param>
    /// <returns>Vector embedding as array of floats</returns>
    Task<float[]> GenerateEmbeddingAsync(string text);
    
    /// <summary>
    /// Generates SQL based on a natural language question
    /// </summary>
    /// <param name="question">Natural language question</param>
    /// <param name="schema">Database schema information</param>
    /// <param name="tenantId">Tenant ID for scoping the query</param>
    /// <returns>Generated SQL query</returns>
    Task<string> GenerateSqlAsync(
        string question,
        string schema,
        string tenantId);
    
    /// <summary>
    /// Extracts structured information from unstructured text
    /// </summary>
    /// <param name="text">Input text</param>
    /// <param name="extractionTemplate">Template defining the structure of the extraction</param>
    /// <returns>Dictionary representing extracted structured data</returns>
    Task<IDictionary<string, object>> ExtractStructuredDataAsync(
        string text,
        string extractionTemplate);
} 