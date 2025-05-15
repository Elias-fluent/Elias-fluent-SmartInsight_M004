using System;
using System.Net;

namespace SmartInsight.AI.Exceptions
{
    /// <summary>
    /// Exception thrown for errors that occur when interacting with the Ollama API.
    /// </summary>
    public class OllamaException : Exception
    {
        /// <summary>
        /// The HTTP status code returned by the Ollama API, if applicable.
        /// </summary>
        public HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// The request that caused the exception, if available.
        /// </summary>
        public string? RequestContent { get; }

        /// <summary>
        /// The response received from the Ollama API, if available.
        /// </summary>
        public string? ResponseContent { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="OllamaException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public OllamaException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OllamaException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception that caused this exception.</param>
        public OllamaException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OllamaException"/> class with HTTP status information.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="statusCode">The HTTP status code returned by the Ollama API.</param>
        /// <param name="responseContent">The response content from the Ollama API, if available.</param>
        public OllamaException(string message, HttpStatusCode statusCode, string? responseContent = null) 
            : base(message)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OllamaException"/> class with HTTP status information.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="statusCode">The HTTP status code returned by the Ollama API.</param>
        /// <param name="requestContent">The request content that caused the error.</param>
        /// <param name="responseContent">The response content from the Ollama API, if available.</param>
        /// <param name="innerException">The inner exception that caused this exception.</param>
        public OllamaException(string message, HttpStatusCode statusCode, string? requestContent, string? responseContent, Exception innerException) 
            : base(message, innerException)
        {
            StatusCode = statusCode;
            RequestContent = requestContent;
            ResponseContent = responseContent;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OllamaException"/> class for connection errors.
        /// </summary>
        /// <param name="innerException">The connection exception.</param>
        /// <returns>An <see cref="OllamaException"/> describing the connection error.</returns>
        public static OllamaException CreateConnectionException(Exception innerException)
        {
            return new OllamaException(
                "Failed to connect to Ollama API server. Ensure the server is running and accessible.",
                innerException
            );
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OllamaException"/> class for timeout errors.
        /// </summary>
        /// <param name="operationName">The name of the operation that timed out.</param>
        /// <param name="innerException">The timeout exception.</param>
        /// <returns>An <see cref="OllamaException"/> describing the timeout error.</returns>
        public static OllamaException CreateTimeoutException(string operationName, Exception innerException)
        {
            return new OllamaException(
                $"The operation '{operationName}' timed out. Consider increasing the timeout value or checking the Ollama server performance.",
                innerException
            );
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OllamaException"/> class for model not found errors.
        /// </summary>
        /// <param name="modelName">The name of the model that was not found.</param>
        /// <returns>An <see cref="OllamaException"/> describing the model not found error.</returns>
        public static OllamaException CreateModelNotFoundException(string modelName)
        {
            return new OllamaException(
                $"The model '{modelName}' was not found. Ensure the model is downloaded or pull it using the PullModelAsync method."
            );
        }
    }
} 