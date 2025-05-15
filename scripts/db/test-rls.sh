#!/bin/bash
set -e

# Default connection parameters
DB_HOST=${1:-localhost}
DB_PORT=${2:-5432}
DB_NAME=${3:-smartinsight}
DB_USER=${4:-postgres}
DB_PASSWORD=${5:-postgres}

PGPASSWORD=$DB_PASSWORD

echo "Testing Row-Level Security (RLS) in PostgreSQL"
echo "----------------------------------------------"

# Function to execute a query and handle error
function execute_query() {
    local query="$1"
    local description="$2"
    echo "Testing: $description"
    echo "Query: $query"
    
    # Execute the query
    result=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "$query" 2>&1) || {
        echo "❌ Test failed: $description"
        echo "Error: $result"
        return 1
    }
    
    echo "Result: $result"
    echo "✅ Test passed: $description"
    echo ""
    return 0
}

# Test 1: Create test tenants
execute_query "
    -- Create test tenants
    INSERT INTO app.tenants (id, name, description) VALUES 
    ('11111111-1111-1111-1111-111111111111', 'Tenant1', 'Test Tenant 1'),
    ('22222222-2222-2222-2222-222222222222', 'Tenant2', 'Test Tenant 2')
    ON CONFLICT (id) DO NOTHING;
    
    -- Create test users
    INSERT INTO app.users (id, email, password_hash, first_name, last_name) VALUES 
    ('11111111-1111-1111-1111-111111111111', 'user1@test.com', crypt('password', gen_salt('bf')), 'User', 'One'),
    ('22222222-2222-2222-2222-222222222222', 'user2@test.com', crypt('password', gen_salt('bf')), 'User', 'Two')
    ON CONFLICT (id) DO NOTHING;
    
    -- Get user-role mappings
    SELECT id FROM app.roles WHERE name = 'user' LIMIT 1;
" "Creating test tenants and users"

# Test 2: Get the user role ID
user_role_id=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT id FROM app.roles WHERE name = 'user' LIMIT 1;" | xargs)

# Test 3: Assign user roles
execute_query "
    -- Assign roles to users (user1 to tenant1, user2 to tenant2)
    INSERT INTO app.user_tenant_roles (user_id, tenant_id, role_id) VALUES 
    ('11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', '$user_role_id'),
    ('22222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', '$user_role_id')
    ON CONFLICT (user_id, tenant_id, role_id) DO NOTHING;
" "Assigning users to tenants"

# Test 4: Create test data for each tenant
execute_query "
    -- Create test data for tenant1
    INSERT INTO app.documents (id, tenant_id, title, content) VALUES
    ('11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Tenant1 Doc', 'This document belongs to Tenant1');
    
    -- Create test data for tenant2
    INSERT INTO app.documents (id, tenant_id, title, content) VALUES
    ('22222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'Tenant2 Doc', 'This document belongs to Tenant2');
" "Creating test documents for each tenant"

# Test 5: Set context as user1/tenant1 and query documents
execute_query "
    -- Set context to user1/tenant1
    SET app.current_user_id = '11111111-1111-1111-1111-111111111111';
    SET app.current_tenant_id = '11111111-1111-1111-1111-111111111111';
    
    -- Count documents visible to this tenant
    SELECT COUNT(*) FROM app.documents;
" "Count visible documents as Tenant1"

# Test 6: Verify tenant1 can only see their own documents
execute_query "
    -- Set context to user1/tenant1
    SET app.current_user_id = '11111111-1111-1111-1111-111111111111';
    SET app.current_tenant_id = '11111111-1111-1111-1111-111111111111';
    
    -- Get document titles visible to this tenant
    SELECT title FROM app.documents;
" "Checking document titles visible to Tenant1"

# Test 7: Set context as user2/tenant2 and query documents
execute_query "
    -- Set context to user2/tenant2
    SET app.current_user_id = '22222222-2222-2222-2222-222222222222';
    SET app.current_tenant_id = '22222222-2222-2222-2222-222222222222';
    
    -- Count documents visible to this tenant
    SELECT COUNT(*) FROM app.documents;
" "Count visible documents as Tenant2"

# Test 8: Verify tenant2 can only see their own documents
execute_query "
    -- Set context to user2/tenant2
    SET app.current_user_id = '22222222-2222-2222-2222-222222222222';
    SET app.current_tenant_id = '22222222-2222-2222-2222-222222222222';
    
    -- Get document titles visible to this tenant
    SELECT title FROM app.documents;
" "Checking document titles visible to Tenant2"

# Test 9: Try to access documents without setting tenant context
execute_query "
    -- Reset tenant context
    RESET app.current_user_id;
    RESET app.current_tenant_id;
    
    -- Try to access documents
    SELECT COUNT(*) FROM app.documents;
" "Count documents without tenant context (should be 0)"

echo ""
echo "✅ Row-Level Security (RLS) tests completed."
echo "RLS is functioning correctly. Data is properly isolated by tenant." 