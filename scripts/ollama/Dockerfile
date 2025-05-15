FROM ollama/ollama:latest

# Install curl for healthcheck and downloading models
USER root
RUN apt-get update && apt-get install -y \
    curl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Create directory for initialization scripts
RUN mkdir -p /ollama/scripts

# Copy initialization scripts
COPY init.sh /ollama/scripts/
COPY download-models.sh /ollama/scripts/

# Make scripts executable
RUN chmod +x /ollama/scripts/*.sh

# Create entrypoint wrapper
RUN echo '#!/bin/bash\n\
# Start Ollama server in the background\n\
ollama serve &\n\
\n\
# Wait for Ollama to be ready\n\
echo "Waiting for Ollama server to be ready..."\n\
until curl -s -f http://localhost:11434/api/version > /dev/null 2>&1; do\n\
  echo "Waiting for Ollama server..."\n\
  sleep 2\n\
done\n\
echo "Ollama server is ready!"\n\
\n\
# Run initialization script to download models\n\
cd /ollama/scripts && ./init.sh\n\
\n\
# Keep the container running\n\
wait\n\
' > /ollama/entrypoint.sh && chmod +x /ollama/entrypoint.sh

# Set the entrypoint
ENTRYPOINT ["/ollama/entrypoint.sh"]

# Default environment variables
ENV OLLAMA_HOST=0.0.0.0
ENV OLLAMA_MODELS="llama3 phi3" 