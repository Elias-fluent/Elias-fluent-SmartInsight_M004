#!/bin/bash
# Ollama Initialization Script

set -e

echo "Starting Ollama initialization..."

# Run the model download script
./download-models.sh

# Check if Ollama models are downloaded correctly
check_models() {
  echo "Verifying models are available..."
  
  # Get list of models
  models_json=$(curl -s http://localhost:11434/api/tags)
  
  # Parse the models JSON to get model names
  if command -v jq >/dev/null 2>&1; then
    # If jq is available, use it for proper JSON parsing
    models=$(echo "$models_json" | jq -r '.models[].name')
  else
    # Fallback to grep+cut for basic extraction
    models=$(echo "$models_json" | grep -o '"name":"[^"]*"' | cut -d'"' -f4)
  fi
  
  missing_models=0
  
  # Check if required models exist
  for model in $OLLAMA_MODELS; do
    if echo "$models" | grep -q "$model"; then
      echo "✅ Model $model is available"
    else
      echo "❌ Model $model is missing"
      missing_models=$((missing_models + 1))
    fi
  done
  
  if [ $missing_models -eq 0 ]; then
    echo "All required models are available."
    return 0
  else
    echo "Warning: $missing_models required models are missing."
    return 1
  fi
}

# Wait a few seconds for model info to be available
sleep 5

# Verify models
check_models

echo "Ollama initialization completed." 