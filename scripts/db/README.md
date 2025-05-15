# PostgreSQL Database with Row-Level Security (RLS)

This directory contains scripts for setting up and configuring the PostgreSQL database with Row-Level Security (RLS) for SmartInsight.

## Directory Structure

- `init.sql`: Main initialization script that sets up the database schema, creates tables, and configures RLS policies.
- `setup-rls.sh`: Example script showing how to set RLS session parameters.
- `test-rls.sh`: Script to test if RLS is working correctly by creating test tenants and checking data isolation.
- `docker-entrypoint-initdb.d/`: Scripts in this directory are automatically executed when the PostgreSQL container starts.

## Row-Level Security Overview

SmartInsight uses PostgreSQL's Row-Level Security (RLS) feature to enforce tenant isolation at the database level. This provides a strong security boundary ensuring that each tenant can only access their own data.

### Key Concepts

1. **Tenant Isolation**: Each row in tenant-scoped tables has a `tenant_id` column.
2. **RLS Policies**: Policies attached to tables filter rows based on the current tenant context.
3. **Session Variables**: The application sets `app.current_user_id` and `app.current_tenant_id` on each database connection.
4. **Automatic Filtering**: Once session variables are set, all queries are automatically filtered.

### Tables with RLS Enabled

- `app.tenants`
- `app.users`
- `app.user_tenant_roles`
- `app.data_sources`
- `app.documents`
- `app.conversation_logs`
- `app.knowledge_nodes`
- `app.relations`
- `app.telemetry_metrics`

## Testing RLS

You can test if RLS is properly configured by running:

```bash
./test-rls.sh [host] [port] [dbname] [user] [password]
```

Default values are used if not provided:
- host: localhost
- port: 5432
- dbname: smartinsight
- user: postgres
- password: postgres

## Configuring the Application

The application must set the RLS session variables on each database connection to enforce tenant isolation. This is typically done through a database interceptor that runs after user authentication.

See `setup-rls.sh` for an example implementation in a .NET application using Entity Framework Core.

## Database Schema

The database schema follows a multi-tenant design pattern:

- Each tenant-scoped table has a `tenant_id` column linking to the `app.tenants` table
- Indices are created on `tenant_id` columns for efficient filtering
- The `app.user_tenant_roles` table defines which users have access to which tenants
- System-wide tables without tenant scoping have special handling in RLS policies 