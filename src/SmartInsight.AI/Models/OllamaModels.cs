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
        /// Additional parameters to control generation.
        /// </summary>
        [JsonPropertyName("options")]
        public Dictionary<string, object>? Options { get; set; }
    }

    /// <summary>
    /// Request to generate a completion from a prompt.
    /// </summary>
    public class OllamaCompletionRequest : OllamaRequestBase
    {
        /// <summary>
        /// The model to use for the request.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null!;

        /// <summary>
        /// The prompt to generate a completion for.
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = null!;
    }

    /// <summary>
    /// Request to generate a completion from a chat history.
    /// </summary>
    public class OllamaChatRequest : OllamaRequestBase
    {
        /// <summary>
        /// The model to use for the request.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null!;

        /// <summary>
        /// The messages to generate a completion from.
        /// </summary>
        [JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; } = null!;
    }

    /// <summary>
    /// A message in a chat conversation.
    /// </summary>
    public class OllamaChatMessage
    {
        /// <summary>
        /// The role of the message sender (system, user, assistant).
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = null!;

        /// <summary>
        /// The content of the message.
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

    /// <summary>
    /// Request to generate an embedding from text.
    /// </summary>
    public class OllamaEmbeddingRequest : OllamaRequestBase
    {
        /// <summary>
        /// The model to use for the request.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null!;

        /// <summary>
        /// The text to generate an embedding for.
        /// </summary>
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = null!;
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
    /// Response from a completion request.
    /// </summary>
    public class OllamaCompletionResponse : OllamaResponseBase
    {
        /// <summary>
        /// The generated completion text.
        /// </summary>
        [JsonPropertyName("response")]
        public string Response { get; set; } = null!;

        /// <summary>
        /// Whether the completion is complete or more tokens will be generated.
        /// </summary>
        [JsonPropertyName("done")]
        public bool Done { get; set; }

        /// <summary>
        /// The number of context tokens used.
        /// </summary>
        [JsonPropertyName("context")]
        public List<int>? Context { get; set; }

        /// <summary>
        /// The number of tokens in the prompt.
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        /// <summary>
        /// The number of tokens in the completion.
        /// </summary>
        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }

        /// <summary>
        /// The time spent evaluating the prompt in nanoseconds.
        /// </summary>
        [JsonPropertyName("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        /// <summary>
        /// The time spent generating the completion in nanoseconds.
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }

        /// <summary>
        /// The total duration of the completion in nanoseconds.
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }
    }

    /// <summary>
    /// Response from a chat completion request.
    /// </summary>
    public class OllamaChatResponse : OllamaResponseBase
    {
        /// <summary>
        /// The message containing the assistant's response.
        /// </summary>
        [JsonPropertyName("message")]
        public OllamaChatMessage Message { get; set; } = null!;

        /// <summary>
        /// Whether the completion is complete or more tokens will be generated.
        /// </summary>
        [JsonPropertyName("done")]
        public bool Done { get; set; }

        /// <summary>
        /// The number of tokens in the prompt.
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        /// <summary>
        /// The number of tokens in the completion.
        /// </summary>
        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }

        /// <summary>
        /// The time spent evaluating the prompt in nanoseconds.
        /// </summary>
        [JsonPropertyName("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        /// <summary>
        /// The time spent generating the completion in nanoseconds.
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }

        /// <summary>
        /// The total duration of the completion in nanoseconds.
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }
    }

    /// <summary>
    /// Response from a request to list models.
    /// </summary>
    public class OllamaModelListResponse
    {
        /// <summary>
        /// The list of available models.
        /// </summary>
        [JsonPropertyName("models")]
        public List<OllamaModelInfo>? Models { get; set; }
    }

    /// <summary>
    /// Information about a model.
    /// </summary>
    public class OllamaModelInfo
    {
        /// <summary>
        /// The name of the model.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The size of the model in bytes.
        /// </summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }

        /// <summary>
        /// The model family.
        /// </summary>
        [JsonPropertyName("family")]
        public string? Family { get; set; }

        /// <summary>
        /// The parameter count of the model.
        /// </summary>
        [JsonPropertyName("parameter_size")]
        public string? ParameterSize { get; set; }

        /// <summary>
        /// The quantization level of the model.
        /// </summary>
        [JsonPropertyName("quantization_level")]
        public string? QuantizationLevel { get; set; }

        /// <summary>
        /// When the model was last modified.
        /// </summary>
        [JsonPropertyName("modified_at")]
        public string? ModifiedAt { get; set; }

        /// <summary>
        /// The format of the model (e.g., gguf).
        /// </summary>
        [JsonPropertyName("format")]
        public string? Format { get; set; }

        /// <summary>
        /// The specifics of a model.
        /// </summary>
        [JsonPropertyName("details")]
        public OllamaModelDetails? Details { get; set; }
    }

    /// <summary>
    /// Details about a model.
    /// </summary>
    public class OllamaModelDetails
    {
        /// <summary>
        /// The name of the model's parent.
        /// </summary>
        [JsonPropertyName("parent_model")]
        public string? ParentModel { get; set; }

        /// <summary>
        /// The format of the model.
        /// </summary>
        [JsonPropertyName("format")]
        public string? Format { get; set; }

        /// <summary>
        /// The family of the model.
        /// </summary>
        [JsonPropertyName("family")]
        public string? Family { get; set; }

        /// <summary>
        /// The number of parameters in the model.
        /// </summary>
        [JsonPropertyName("parameter_size")]
        public string? ParameterSize { get; set; }

        /// <summary>
        /// The quantization level of the model.
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
        /// The status of the operation.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// The current progress in bytes.
        /// </summary>
        [JsonPropertyName("digest")]
        public string? Digest { get; set; }

        /// <summary>
        /// The total size in bytes.
        /// </summary>
        [JsonPropertyName("total")]
        public long Total { get; set; }

        /// <summary>
        /// The current download or build progress.
        /// </summary>
        [JsonPropertyName("completed")]
        public long Completed { get; set; }
    }

    /// <summary>
    /// Response from an embedding request.
    /// </summary>
    public class OllamaEmbeddingResponse
    {
        /// <summary>
        /// The model used for the embedding.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = null!;

        /// <summary>
        /// The embedding vector.
        /// </summary>
        [JsonPropertyName("embedding")]
        public List<float> Embedding { get; set; } = null!;
        
        /// <summary>
        /// The number of tokens in the prompt.
        /// </summary>
        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }
        
        /// <summary>
        /// The time spent evaluating the prompt in nanoseconds.
        /// </summary>
        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }
        
        /// <summary>
        /// The total duration of generating the embedding in nanoseconds.
        /// </summary>
        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }
    }

    #endregion
} 