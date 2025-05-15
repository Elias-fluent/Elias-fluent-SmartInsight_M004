using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartInsight.AI.Models
{
    #region Request Models

    /// <summary>
    /// Base class for Ollama API requests
    /// </summary>
    public abstract class OllamaRequestBase
    {
        /// <summary>
        /// The model to use for the request
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null!;
    }

    /// <summary>
    /// Request to generate completions
    /// </summary>
    public class OllamaCompletionRequest : OllamaRequestBase
    {
        /// <summary>
        /// The prompt to generate a completion for
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = null!;

        /// <summary>
        /// Whether to stream the response
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Additional parameters for the request
        /// </summary>
        /// <remarks>
        /// This can include parameters like temperature, top_p, top_k, etc.
        /// </remarks>
        [JsonExtensionData]
        public Dictionary<string, object>? Options { get; set; }
    }

    /// <summary>
    /// Request for chat completions
    /// </summary>
    public class OllamaChatRequest : OllamaRequestBase
    {
        /// <summary>
        /// The chat messages to generate a completion for
        /// </summary>
        [JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; } = new();

        /// <summary>
        /// Whether to stream the response
        /// </summary>
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        /// <summary>
        /// Additional parameters for the request
        /// </summary>
        /// <remarks>
        /// This can include parameters like temperature, top_p, top_k, etc.
        /// </remarks>
        [JsonExtensionData]
        public Dictionary<string, object>? Options { get; set; }
    }

    /// <summary>
    /// A chat message in a chat completion request
    /// </summary>
    public class OllamaChatMessage
    {
        /// <summary>
        /// The role of the message sender (system, user, assistant)
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = null!;

        /// <summary>
        /// The content of the message
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = null!;
    }

    /// <summary>
    /// Request to pull a model
    /// </summary>
    public class OllamaModelPullRequest
    {
        /// <summary>
        /// The name of the model to pull
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Whether to skip downloading if model exists
        /// </summary>
        [JsonPropertyName("insecure")]
        public bool Insecure { get; set; } = false;
    }

    /// <summary>
    /// Request to create a model
    /// </summary>
    public class OllamaModelCreateRequest
    {
        /// <summary>
        /// The name to give the model
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The model definition (Modelfile content)
        /// </summary>
        [JsonPropertyName("modelfile")]
        public string Modelfile { get; set; } = null!;

        /// <summary>
        /// The path to build the model from
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }

    /// <summary>
    /// Request to delete a model
    /// </summary>
    public class OllamaModelDeleteRequest
    {
        /// <summary>
        /// The name of the model to delete
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }

    #endregion

    #region Response Models

    /// <summary>
    /// Base response from Ollama API
    /// </summary>
    public class OllamaResponseBase
    {
        /// <summary>
        /// The model used for the response
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null!;

        /// <summary>
        /// Creation timestamp
        /// </summary>
        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = null!;
    }

    /// <summary>
    /// Response from completion endpoint
    /// </summary>
    public class OllamaCompletionResponse : OllamaResponseBase
    {
        /// <summary>
        /// The generated completion
        /// </summary>
        [JsonPropertyName("response")]
        public string Response { get; set; } = null!;

        /// <summary>
        /// Whether this is a final response in a stream
        /// </summary>
        [JsonPropertyName("done")]
        public bool Done { get; set; }

        /// <summary>
        /// The prompt that was provided
        /// </summary>
        [JsonPropertyName("prompt")]
        public string? Prompt { get; set; }

        /// <summary>
        /// Total processing time in nanoseconds
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; }

        /// <summary>
        /// Duration for loading the model in nanoseconds
        /// </summary>
        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; }

        /// <summary>
        /// Duration for prompt evaluation in nanoseconds
        /// </summary>
        [JsonPropertyName("prompt_eval_duration")]
        public long? PromptEvalDuration { get; set; }

        /// <summary>
        /// Tokens processed per second
        /// </summary>
        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }

        /// <summary>
        /// Tokens per second
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; }
    }

    /// <summary>
    /// Response from chat completion endpoint
    /// </summary>
    public class OllamaChatResponse : OllamaResponseBase
    {
        /// <summary>
        /// The message containing the generated response
        /// </summary>
        [JsonPropertyName("message")]
        public OllamaChatMessage Message { get; set; } = null!;

        /// <summary>
        /// Whether this is a final response in a stream
        /// </summary>
        [JsonPropertyName("done")]
        public bool Done { get; set; }

        /// <summary>
        /// Total processing time in nanoseconds
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long? TotalDuration { get; set; }

        /// <summary>
        /// Duration for loading the model in nanoseconds
        /// </summary>
        [JsonPropertyName("load_duration")]
        public long? LoadDuration { get; set; }

        /// <summary>
        /// Duration for prompt evaluation in nanoseconds
        /// </summary>
        [JsonPropertyName("prompt_eval_duration")]
        public long? PromptEvalDuration { get; set; }

        /// <summary>
        /// Tokens processed per second
        /// </summary>
        [JsonPropertyName("eval_count")]
        public int? EvalCount { get; set; }

        /// <summary>
        /// Tokens per second
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long? EvalDuration { get; set; }
    }

    /// <summary>
    /// Response from model list endpoint
    /// </summary>
    public class OllamaModelListResponse
    {
        /// <summary>
        /// The list of models
        /// </summary>
        [JsonPropertyName("models")]
        public List<OllamaModelInfo> Models { get; set; } = new();
    }

    /// <summary>
    /// Information about a model
    /// </summary>
    public class OllamaModelInfo
    {
        /// <summary>
        /// The name of the model
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The model's size in bytes
        /// </summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }

        /// <summary>
        /// When the model was modified
        /// </summary>
        [JsonPropertyName("modified_at")]
        public string ModifiedAt { get; set; } = null!;

        /// <summary>
        /// Digest of the model
        /// </summary>
        [JsonPropertyName("digest")]
        public string? Digest { get; set; }

        /// <summary>
        /// Parameter count
        /// </summary>
        [JsonPropertyName("parameter_size")]
        public string? ParameterSize { get; set; }

        /// <summary>
        /// Quantization level
        /// </summary>
        [JsonPropertyName("quantization_level")]
        public string? QuantizationLevel { get; set; }
    }

    /// <summary>
    /// Response for model status during operations like pull
    /// </summary>
    public class OllamaModelStatusResponse
    {
        /// <summary>
        /// Status message
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        /// <summary>
        /// Progress as a percentage (0-100)
        /// </summary>
        [JsonPropertyName("progress")]
        public double? Progress { get; set; }

        /// <summary>
        /// Whether the operation is complete
        /// </summary>
        [JsonPropertyName("completed")]
        public bool? Completed { get; set; }
    }

    #endregion
} 