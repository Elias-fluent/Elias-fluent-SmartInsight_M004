#!/bin/bash
set -e

# Run the SQL initialization script as the postgres user
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  \echo 'Initializing SmartInsight database with Row-Level Security (RLS)'
  \i /docker-entrypoint-initdb.d/init.sql
EOSQL

echo "SmartInsight database initialized successfully with Row-Level Security enabled." 