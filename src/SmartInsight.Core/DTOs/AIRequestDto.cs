namespace SmartInsight.Core.DTOs;

/// <summary>
/// DTO for AI text generation request
/// </summary>
public record TextGenerationRequestDto
{
    /// <summary>
    /// Input prompt for text generation
    /// </summary>
    public required string Prompt { get; init; }
    
    /// <summary>
    /// Optional system prompt for context
    /// </summary>
    public string? SystemPrompt { get; init; }
    
    /// <summary>
    /// Optional conversation history
    /// </summary>
    public List<MessageDto>? ConversationHistory { get; init; }
    
    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int MaxTokens { get; init; } = 1024;
    
    /// <summary>
    /// Temperature for sampling (0.0 to 1.0)
    /// </summary>
    public float Temperature { get; init; } = 0.7f;
}

/// <summary>
/// DTO for AI conversation message
/// </summary>
public record MessageDto
{
    /// <summary>
    /// Role of the message author (e.g. 'user', 'assistant', 'system')
    /// </summary>
    public required string Role { get; init; }
    
    /// <summary>
    /// Content of the message
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// DTO for SQL generation request
/// </summary>
public record SqlGenerationRequestDto
{
    /// <summary>
    /// Natural language question to convert to SQL
    /// </summary>
    public required string Question { get; init; }
    
    /// <summary>
    /// Database schema information
    /// </summary>
    public required string Schema { get; init; }
    
    /// <summary>
    /// Tenant ID for scoping the query
    /// </summary>
    public required string TenantId { get; init; }
}

/// <summary>
/// DTO for embedding generation request
/// </summary>
public record EmbeddingRequestDto
{
    /// <summary>
    /// Text to generate embedding for
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// DTO for AI operation response
/// </summary>
public record AIResponseDto
{
    /// <summary>
    /// Generated text
    /// </summary>
    public string? Text { get; init; }
    
    /// <summary>
    /// Generated SQL query
    /// </summary>
    public string? SqlQuery { get; init; }
    
    /// <summary>
    /// Generated embedding
    /// </summary>
    public float[]? Embedding { get; init; }
    
    /// <summary>
    /// Structured data extracted from text
    /// </summary>
    public IDictionary<string, object>? StructuredData { get; init; }
    
    /// <summary>
    /// Tokens used in the request
    /// </summary>
    public int InputTokens { get; init; }
    
    /// <summary>
    /// Tokens generated in the response
    /// </summary>
    public int OutputTokens { get; init; }
    
    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; init; }
} 