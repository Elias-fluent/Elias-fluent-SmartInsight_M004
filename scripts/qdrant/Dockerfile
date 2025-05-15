FROM qdrant/qdrant:latest

# Arguments for customization
ARG NODE_VERSION=18

# Install dependencies
USER root
RUN apt-get update && apt-get install -y \
    curl \
    gnupg \
    ca-certificates \
    nodejs \
    npm \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Create directory for initialization scripts
RUN mkdir -p /qdrant/scripts

# Copy configuration and initialization scripts
COPY config.yaml /qdrant/config/production.yaml
COPY init.sh /qdrant/scripts/
COPY init-collections.js /qdrant/scripts/

# Make the initialization script executable
RUN chmod +x /qdrant/scripts/init.sh

# Create entrypoint wrapper script
RUN echo '#!/bin/bash\n\
# Start Qdrant in the background\n\
/qdrant/entrypoint.sh &\n\
\n\
# Wait for Qdrant to be ready and run initialization\n\
cd /qdrant/scripts && ./init.sh\n\
\n\
# Keep the container running\n\
wait\n\
' > /qdrant/custom-entrypoint.sh && chmod +x /qdrant/custom-entrypoint.sh

# Switch back to qdrant user for security
USER qdrant

# Set the entrypoint to our custom script
ENTRYPOINT ["/qdrant/custom-entrypoint.sh"] 