#!/bin/bash
# Model Download Script for Ollama

set -e

# Get the list of models to download
MODELS=${OLLAMA_MODELS:-"llama3 phi3"}

echo "Will download the following models: $MODELS"

# Function to download a model
download_model() {
  model=$1
  echo "Downloading $model model..."
  
  # Pull the model
  ollama pull $model
  
  # Check if download was successful
  if [ $? -eq 0 ]; then
    echo "Successfully downloaded $model"
  else
    echo "Failed to download $model"
    return 1
  fi
}

# Function to wait for Ollama to be ready
wait_for_ollama() {
  echo "Ensuring Ollama server is running..."
  
  retries=30
  while [ $retries -gt 0 ]; do
    if curl -s -f "http://localhost:11434/api/version" >/dev/null 2>&1; then
      echo "Ollama server is ready!"
      return 0
    fi
    retries=$((retries - 1))
    echo "Waiting for Ollama server... ($retries retries left)"
    sleep 2
  done
  
  echo "Failed to connect to Ollama server after multiple attempts"
  return 1
}

# Wait for Ollama to be ready
wait_for_ollama

# Loop through and download each model
for model in $MODELS; do
  download_model $model
done

echo "Model download completed." 