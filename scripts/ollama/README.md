# Ollama Container for Local AI Inference

This directory contains scripts and configuration for setting up the Ollama container for local AI inference in SmartInsight.

## Directory Structure

- `Dockerfile`: Custom Ollama image with initialization scripts
- `init.sh`: Shell script to initialize Ollama environment
- `download-models.sh`: Script to download AI models

## Features

- **Zero-cloud dependency**: All AI inference runs locally
- **Pre-downloaded models**: LLaMA 3 and Phi3 models available immediately
- **Custom initialization**: Scripts for model downloading and verification
- **Health checks**: Ensures the service is available before being used
- **Volume persistence**: Preserves downloaded models between container restarts

## Configuration

The Ollama container can be configured using the following environment variables:

- `OLLAMA_HOST`: Host to bind the Ollama server to (default: 0.0.0.0)
- `OLLAMA_MODELS`: Space-separated list of models to download (default: "llama3 phi3")

## Usage in Application

When integrating with Ollama in your application code, use the Ollama API endpoint:

```csharp
// Example C# code
var ollamaClient = new OllamaClient(new Uri("http://ollama:11434"));
var response = await ollamaClient.GenerateAsync(new GenerationRequest 
{
    Model = "llama3",
    Prompt = "What is the capital of France?"
});
```

## Resource Requirements

Ollama uses significant resources for AI inference:

- **CPU**: At least 4 cores recommended
- **RAM**: Minimum 8GB available memory (16GB+ recommended)
- **Storage**: 10-15GB per model

## Model Details

### LLaMA 3 (Default)

- **Type**: General-purpose large language model
- **Size**: ~8GB
- **Performance**: Best quality responses, higher resource usage

### Phi3 (Fallback)

- **Type**: Lightweight language model
- **Size**: ~4GB
- **Performance**: Lower resource usage, suitable for simpler queries

## Testing

You can test the Ollama container using the Ollama CLI or API:

```bash
# Using curl
curl -X POST http://localhost:11434/api/generate -d '{
  "model": "llama3",
  "prompt": "What is the capital of France?"
}'
```

## Troubleshooting

- **Memory Issues**: If container crashes, increase Docker memory limits
- **Model Download Failures**: Check network connection and retry
- **API Connection Problems**: Verify port mappings and network settings 