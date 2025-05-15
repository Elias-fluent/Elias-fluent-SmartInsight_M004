#!/bin/bash
# Qdrant Initialization Script

set -e

# Configuration
QDRANT_HOST=${QDRANT_HOST:-localhost}
QDRANT_PORT=${QDRANT_PORT:-6333}
QDRANT_URL="http://${QDRANT_HOST}:${QDRANT_PORT}"

# Wait for Qdrant to be ready
wait_for_qdrant() {
  echo "Waiting for Qdrant to be ready..."
  
  retries=30
  while [ $retries -gt 0 ]; do
    if curl -s -f "${QDRANT_URL}/healthz" >/dev/null 2>&1; then
      echo "Qdrant is ready!"
      return 0
    fi
    retries=$((retries - 1))
    echo "Waiting for Qdrant to be ready... ($retries retries left)"
    sleep 2
  done
  
  echo "Failed to connect to Qdrant after multiple attempts"
  return 1
}

# Create collections with tenant isolation
initialize_collections() {
  echo "Initializing Qdrant collections with tenant namespaces..."
  
  # Install node-fetch if needed
  if ! npm list -g node-fetch >/dev/null 2>&1; then
    echo "Installing node-fetch..."
    npm install -g node-fetch
  fi
  
  # Run the initialization script
  QDRANT_URL="${QDRANT_URL}" node ./init-collections.js
}

# Test tenant isolation
test_tenant_isolation() {
  echo "Testing tenant isolation in Qdrant collections..."
  
  # Create test vectors for different tenants
  tenant1_id="tenant1"
  tenant2_id="tenant2"
  
  # Create random test vectors
  test_vector_json=$(cat <<EOF
{
  "points": [
    {
      "id": "test_tenant1_vector",
      "vector": [0.1, 0.2, 0.3, 0.4, 0.5],
      "payload": {
        "tenant_id": "${tenant1_id}",
        "content": "This is a test document for tenant 1"
      }
    },
    {
      "id": "test_tenant2_vector",
      "payload": {
        "tenant_id": "${tenant2_id}",
        "content": "This is a test document for tenant 2"
      },
      "vector": [0.2, 0.3, 0.4, 0.5, 0.6]
    }
  ]
}
EOF
)
  
  # Create test collection with 5-dimensional vectors
  echo "Creating test collection..."
  curl -s -X PUT "${QDRANT_URL}/collections/test_tenant_isolation" \
    -H "Content-Type: application/json" \
    -d '{
      "vectors": {
        "size": 5,
        "distance": "Cosine"
      }
    }'
  
  # Add payload index for tenant_id
  echo "Adding tenant_id index..."
  curl -s -X PUT "${QDRANT_URL}/collections/test_tenant_isolation/index" \
    -H "Content-Type: application/json" \
    -d '{
      "field_name": "tenant_id", 
      "field_schema": "keyword"
    }'
  
  # Insert test vectors
  echo "Inserting test vectors for different tenants..."
  curl -s -X PUT "${QDRANT_URL}/collections/test_tenant_isolation/points" \
    -H "Content-Type: application/json" \
    -d "$test_vector_json"
  
  # Query for tenant1 vectors
  echo "Querying for tenant1 vectors..."
  tenant1_results=$(curl -s -X POST "${QDRANT_URL}/collections/test_tenant_isolation/points/search" \
    -H "Content-Type: application/json" \
    -d '{
      "vector": [0.1, 0.2, 0.3, 0.4, 0.5],
      "filter": {
        "must": [
          {
            "key": "tenant_id",
            "match": { "value": "tenant1" }
          }
        ]
      },
      "limit": 10
    }')
  
  # Query for tenant2 vectors
  echo "Querying for tenant2 vectors..."
  tenant2_results=$(curl -s -X POST "${QDRANT_URL}/collections/test_tenant_isolation/points/search" \
    -H "Content-Type: application/json" \
    -d '{
      "vector": [0.1, 0.2, 0.3, 0.4, 0.5],
      "filter": {
        "must": [
          {
            "key": "tenant_id",
            "match": { "value": "tenant2" }
          }
        ]
      },
      "limit": 10
    }')
  
  # Check if tenant1 results contain only tenant1 vectors
  tenant1_count=$(echo "$tenant1_results" | grep -o "test_tenant1_vector" | wc -l)
  tenant2_in_tenant1=$(echo "$tenant1_results" | grep -o "test_tenant2_vector" | wc -l)
  
  if [ "$tenant1_count" -gt 0 ] && [ "$tenant2_in_tenant1" -eq 0 ]; then
    echo "✅ Tenant isolation test passed for tenant1: Only tenant1 vectors returned"
  else
    echo "❌ Tenant isolation test failed for tenant1"
    echo "tenant1 results: $tenant1_results"
  fi
  
  # Check if tenant2 results contain only tenant2 vectors
  tenant2_count=$(echo "$tenant2_results" | grep -o "test_tenant2_vector" | wc -l)
  tenant1_in_tenant2=$(echo "$tenant2_results" | grep -o "test_tenant1_vector" | wc -l)
  
  if [ "$tenant2_count" -gt 0 ] && [ "$tenant1_in_tenant2" -eq 0 ]; then
    echo "✅ Tenant isolation test passed for tenant2: Only tenant2 vectors returned"
  else
    echo "❌ Tenant isolation test failed for tenant2"
    echo "tenant2 results: $tenant2_results"
  fi
  
  # Clean up test collection
  echo "Cleaning up test collection..."
  curl -s -X DELETE "${QDRANT_URL}/collections/test_tenant_isolation"
}

# Main execution
main() {
  wait_for_qdrant
  initialize_collections
  test_tenant_isolation
  
  echo "Qdrant initialization completed successfully"
}

# Run the script
main 