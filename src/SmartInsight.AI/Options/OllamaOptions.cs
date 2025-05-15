using System;
using System.Collections.Generic;

namespace SmartInsight.AI.Options
{
    /// <summary>
    /// Configuration options for the Ollama client.
    /// </summary>
    public class OllamaOptions
    {
        /// <summary>
        /// Base URL for the Ollama API. Default is http://localhost:11434/api.
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:11434/api";

        /// <summary>
        /// Default timeout in seconds for API requests. Default is 30 seconds.
        /// </summary>
        public int DefaultTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum number of retry attempts for failed requests. Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retry attempts in milliseconds. Default is 1000ms (1 second).
        /// </summary>
        public int RetryDelayMilliseconds { get; set; } = 1000;

        /// <summary>
        /// Primary model to use for inference. Default is "llama3".
        /// </summary>
        public string PrimaryModel { get; set; } = "llama3";
        
        /// <summary>
        /// Default model to use for operations. Uses the value of PrimaryModel.
        /// </summary>
        public string DefaultModel => PrimaryModel;

        /// <summary>
        /// Fallback model to use when primary model fails. Default is "phi3".
        /// </summary>
        public string FallbackModel { get; set; } = "phi3";

        /// <summary>
        /// Whether to use fallback model when primary model fails. Default is true.
        /// </summary>
        public bool EnableFallback { get; set; } = true;

        /// <summary>
        /// Default parameters for model inference.
        /// </summary>
        public Dictionary<string, object> DefaultParameters { get; set; } = new Dictionary<string, object>
        {
            { "temperature", 0.7 },
            { "top_p", 0.9 },
            { "top_k", 40 },
            { "num_predict", 128 },
            { "stream", false }
        };

        /// <summary>
        /// Maximum supported context length in tokens. This varies by model but we set a conservative default.
        /// </summary>
        public int MaxContextLength { get; set; } = 4096;
        
        /// <summary>
        /// Batch size for processing multiple items in parallel. Default is 10.
        /// </summary>
        public int BatchSize { get; set; } = 10;
    }
} 