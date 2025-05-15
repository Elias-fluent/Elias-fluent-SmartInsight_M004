-- Initialize SmartInsight database with Row-Level Security (RLS)
-- This script sets up the initial database schema and RLS policies

-- Enable Row-Level Security
ALTER DATABASE smartinsight SET row_security = on;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create schema
CREATE SCHEMA IF NOT EXISTS app;

-- Create Tenants table
CREATE TABLE IF NOT EXISTS app.tenants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Create Roles table
CREATE TABLE IF NOT EXISTS app.roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Insert default roles
INSERT INTO app.roles (name, description) VALUES
    ('admin', 'System administrator with full access'),
    ('user', 'Regular user with standard permissions'),
    ('analyst', 'Data analyst with extended query permissions')
ON CONFLICT (id) DO NOTHING;

-- Create Users table with tenant relation
CREATE TABLE IF NOT EXISTS app.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(50),
    last_name VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMPTZ,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Create User-Tenant-Role assignments
CREATE TABLE IF NOT EXISTS app.user_tenant_roles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES app.users(id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL REFERENCES app.tenants(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES app.roles(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, tenant_id, role_id)
);

-- Create Data Sources table
CREATE TABLE IF NOT EXISTS app.data_sources (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES app.tenants(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    source_type VARCHAR(50) NOT NULL, -- e.g., 'postgresql', 'mssql', 'file', 'api'
    connection_string TEXT, -- encrypted connection details
    credentials TEXT, -- encrypted auth credentials
    refresh_schedule VARCHAR(50), -- cron expression
    last_refresh TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Create Documents table
CREATE TABLE IF NOT EXISTS app.documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES app.tenants(id) ON DELETE CASCADE,
    data_source_id UUID REFERENCES app.data_sources(id) ON DELETE SET NULL,
    title VARCHAR(255) NOT NULL,
    content TEXT,
    content_type VARCHAR(50), -- e.g., 'text', 'pdf', 'json'
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create Conversation Logs table
CREATE TABLE IF NOT EXISTS app.conversation_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES app.tenants(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES app.users(id) ON DELETE CASCADE,
    session_id UUID NOT NULL,
    query TEXT NOT NULL,
    response TEXT,
    context JSONB,
    feedback SMALLINT, -- null, 1 (positive), -1 (negative)
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create Knowledge Nodes table
CREATE TABLE IF NOT EXISTS app.knowledge_nodes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES app.tenants(id) ON DELETE CASCADE,
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(255) NOT NULL,
    properties JSONB,
    vector_id VARCHAR(255), -- Reference to vector in Qdrant
    source_document_id UUID REFERENCES app.documents(id) ON DELETE SET NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, entity_type, entity_id)
);

-- Create Relations table
CREATE TABLE IF NOT EXISTS app.relations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES app.tenants(id) ON DELETE CASCADE,
    source_node_id UUID NOT NULL REFERENCES app.knowledge_nodes(id) ON DELETE CASCADE,
    target_node_id UUID NOT NULL REFERENCES app.knowledge_nodes(id) ON DELETE CASCADE,
    relation_type VARCHAR(100) NOT NULL,
    properties JSONB,
    confidence FLOAT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create Telemetry metrics table
CREATE TABLE IF NOT EXISTS app.telemetry_metrics (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID REFERENCES app.tenants(id) ON DELETE SET NULL,
    user_id UUID REFERENCES app.users(id) ON DELETE SET NULL,
    event_type VARCHAR(100) NOT NULL,
    event_data JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create admin user function (to be used in initialization)
CREATE OR REPLACE FUNCTION app.create_admin_user(
    p_email VARCHAR(255),
    p_password VARCHAR(255),
    p_first_name VARCHAR(50),
    p_last_name VARCHAR(50),
    p_tenant_name VARCHAR(100)
) RETURNS TABLE (
    user_id UUID,
    tenant_id UUID
) AS $$
DECLARE
    v_user_id UUID;
    v_tenant_id UUID;
    v_admin_role_id UUID;
BEGIN
    -- Get admin role ID
    SELECT id INTO v_admin_role_id FROM app.roles WHERE name = 'admin';
    
    -- Create tenant
    INSERT INTO app.tenants (name, description)
    VALUES (p_tenant_name, 'Default tenant for ' || p_tenant_name)
    RETURNING id INTO v_tenant_id;
    
    -- Create user with hashed password
    INSERT INTO app.users (email, password_hash, first_name, last_name)
    VALUES (p_email, crypt(p_password, gen_salt('bf')), p_first_name, p_last_name)
    RETURNING id INTO v_user_id;
    
    -- Assign admin role to user for the tenant
    INSERT INTO app.user_tenant_roles (user_id, tenant_id, role_id)
    VALUES (v_user_id, v_tenant_id, v_admin_role_id);
    
    RETURN QUERY SELECT v_user_id, v_tenant_id;
END;
$$ LANGUAGE plpgsql;

-- Create system current_tenant function (used by RLS policies)
CREATE OR REPLACE FUNCTION app.current_tenant_id() RETURNS UUID AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_tenant_id', TRUE), '');
EXCEPTION
    WHEN OTHERS THEN
        RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Create system current_user function (used by RLS policies)
CREATE OR REPLACE FUNCTION app.current_user_id() RETURNS UUID AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_user_id', TRUE), '');
EXCEPTION
    WHEN OTHERS THEN
        RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- Create check_user_tenant_access function
CREATE OR REPLACE FUNCTION app.check_user_tenant_access(
    p_user_id UUID,
    p_tenant_id UUID
) RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM app.user_tenant_roles 
        WHERE user_id = p_user_id AND tenant_id = p_tenant_id
    );
END;
$$ LANGUAGE plpgsql;

-- Enable Row-Level Security on all tenant-scoped tables
ALTER TABLE app.tenants ENABLE ROW LEVEL SECURITY;
ALTER TABLE app.users ENABLE ROW LEVEL SECURITY;
ALTER TABLE app.user_tenant_roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE app.data_sources ENABLE ROW LEVEL SECURITY;
ALTER TABLE app.documents ENABLE ROW LEVEL SECURITY;
ALTER TABLE app.conversation_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE app.knowledge_nodes ENABLE ROW LEVEL SECURITY;
ALTER TABLE app.relations ENABLE ROW LEVEL SECURITY;
ALTER TABLE app.telemetry_metrics ENABLE ROW LEVEL SECURITY;

-- Create RLS policies for each table

-- For tenants table (users can only see tenants they have access to)
CREATE POLICY tenant_isolation_policy ON app.tenants
    USING (id IN (
        SELECT tenant_id FROM app.user_tenant_roles 
        WHERE user_id = app.current_user_id()
    ));

-- For data_sources table
CREATE POLICY tenant_isolation_policy ON app.data_sources
    USING (tenant_id = app.current_tenant_id());

-- For documents table
CREATE POLICY tenant_isolation_policy ON app.documents
    USING (tenant_id = app.current_tenant_id());

-- For conversation_logs table
CREATE POLICY tenant_isolation_policy ON app.conversation_logs
    USING (tenant_id = app.current_tenant_id());

-- For knowledge_nodes table
CREATE POLICY tenant_isolation_policy ON app.knowledge_nodes
    USING (tenant_id = app.current_tenant_id());

-- For relations table
CREATE POLICY tenant_isolation_policy ON app.relations
    USING (tenant_id = app.current_tenant_id());

-- For telemetry_metrics table
CREATE POLICY tenant_isolation_policy ON app.telemetry_metrics
    USING (tenant_id IS NULL OR tenant_id = app.current_tenant_id());

-- For users table (users can see their own records + users in same tenants if admin)
CREATE POLICY user_isolation_policy ON app.users
    USING (
        id = app.current_user_id() OR 
        id IN (
            SELECT u.user_id FROM app.user_tenant_roles u
            JOIN app.user_tenant_roles current_user ON 
                current_user.tenant_id = u.tenant_id AND
                current_user.user_id = app.current_user_id() AND
                current_user.role_id IN (SELECT id FROM app.roles WHERE name = 'admin')
        )
    );

-- For user_tenant_roles table
CREATE POLICY user_tenant_role_isolation_policy ON app.user_tenant_roles
    USING (
        user_id = app.current_user_id() OR
        tenant_id IN (
            SELECT u.tenant_id FROM app.user_tenant_roles u
            WHERE u.user_id = app.current_user_id() AND
                u.role_id IN (SELECT id FROM app.roles WHERE name = 'admin')
        )
    );

-- Create initial default admin user and tenant
SELECT app.create_admin_user(
    'admin@smartinsight.local',
    'Admin@123',
    'System',
    'Administrator',
    'Default'
);

-- Create indices for better performance
CREATE INDEX idx_documents_tenant ON app.documents(tenant_id);
CREATE INDEX idx_knowledge_nodes_tenant ON app.knowledge_nodes(tenant_id);
CREATE INDEX idx_relations_tenant ON app.relations(tenant_id);
CREATE INDEX idx_datasources_tenant ON app.data_sources(tenant_id);
CREATE INDEX idx_conversation_logs_tenant_user ON app.conversation_logs(tenant_id, user_id);
CREATE INDEX idx_user_tenant_roles_user ON app.user_tenant_roles(user_id);
CREATE INDEX idx_user_tenant_roles_tenant ON app.user_tenant_roles(tenant_id); 