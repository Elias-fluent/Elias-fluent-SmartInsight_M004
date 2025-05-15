using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using System.Text;

namespace SmartInsight.Data.Configurations;

/// <summary>
/// Provides utilities for managing PostgreSQL Row-Level Security (RLS) policies
/// </summary>
public static class PostgresRlsPolicy
{
    /// <summary>
    /// Creates RLS policy operations for a tenant-isolated table
    /// </summary>
    /// <param name="migrationBuilder">The migration builder</param>
    /// <param name="tableName">Table to apply RLS to</param>
    /// <param name="schemaName">Schema containing the table (optional)</param>
    /// <returns>Operation builder for method chaining</returns>
    public static OperationBuilder<SqlOperation> CreateTenantPolicy(
        this MigrationBuilder migrationBuilder,
        string tableName,
        string schemaName = "smartinsight")
    {
        // Fully qualified table name with schema
        string fullTableName = string.IsNullOrEmpty(schemaName) 
            ? tableName 
            : $"{schemaName}.{tableName}";

        // Enable RLS on the table
        migrationBuilder.Sql($"ALTER TABLE {fullTableName} ENABLE ROW LEVEL SECURITY;");

        // Create SQL for the RLS policy
        string policyName = $"{tableName}_tenant_isolation_policy";

        // Create policy that allows users to see only rows for their tenant
        // The tenant_id() function will be created in a separate migration
        return migrationBuilder.Sql(
            $@"CREATE POLICY {policyName} ON {fullTableName}
                USING (tenant_id() IS NULL OR tenant_id() = '00000000-0000-0000-0000-000000000000' OR ""TenantId"" = tenant_id())
                WITH CHECK (tenant_id() IS NULL OR tenant_id() = '00000000-0000-0000-0000-000000000000' OR ""TenantId"" = tenant_id());");
    }

    /// <summary>
    /// Drops an RLS policy for a tenant-isolated table
    /// </summary>
    /// <param name="migrationBuilder">The migration builder</param>
    /// <param name="tableName">Table to remove RLS from</param>
    /// <param name="schemaName">Schema containing the table (optional)</param>
    /// <returns>Operation builder for method chaining</returns>
    public static OperationBuilder<SqlOperation> DropTenantPolicy(
        this MigrationBuilder migrationBuilder,
        string tableName,
        string schemaName = "smartinsight")
    {
        // Fully qualified table name with schema
        string fullTableName = string.IsNullOrEmpty(schemaName) 
            ? tableName 
            : $"{schemaName}.{tableName}";

        string policyName = $"{tableName}_tenant_isolation_policy";

        // Drop the policy and disable RLS
        migrationBuilder.Sql($"DROP POLICY IF EXISTS {policyName} ON {fullTableName};");
        return migrationBuilder.Sql($"ALTER TABLE {fullTableName} DISABLE ROW LEVEL SECURITY;");
    }

    /// <summary>
    /// Creates the tenant_id() PostgreSQL function for RLS
    /// </summary>
    /// <param name="migrationBuilder">The migration builder</param>
    /// <returns>Operation builder for method chaining</returns>
    public static OperationBuilder<SqlOperation> CreateTenantIdFunction(
        this MigrationBuilder migrationBuilder)
    {
        return migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION tenant_id() RETURNS uuid AS $$
            BEGIN
                -- Return the current tenant ID from the app.current_tenant setting
                -- This setting should be set at the connection level by the application
                RETURN nullif(current_setting('app.current_tenant', TRUE), '')::uuid;
            EXCEPTION
                WHEN OTHERS THEN
                    -- Return NULL if the setting isn't defined
                    RETURN NULL;
            END;
            $$ LANGUAGE plpgsql SECURITY DEFINER;");
    }

    /// <summary>
    /// Drops the tenant_id() PostgreSQL function
    /// </summary>
    /// <param name="migrationBuilder">The migration builder</param>
    /// <returns>Operation builder for method chaining</returns>
    public static OperationBuilder<SqlOperation> DropTenantIdFunction(
        this MigrationBuilder migrationBuilder)
    {
        return migrationBuilder.Sql("DROP FUNCTION IF EXISTS tenant_id();");
    }
    
    /// <summary>
    /// Applies RLS policies to all tenant-isolated tables in the database
    /// </summary>
    /// <param name="migrationBuilder">The migration builder</param>
    /// <returns>Operation builder for method chaining</returns>
    public static OperationBuilder<SqlOperation> ApplyRlsToAllTenantTables(
        this MigrationBuilder migrationBuilder)
    {
        // Create the tenant_id function first
        migrationBuilder.CreateTenantIdFunction();
        
        // SQL to find all tenant-isolated tables (those with a TenantId column)
        string sql = @"
            SELECT 
                table_schema,
                table_name
            FROM 
                information_schema.columns
            WHERE 
                column_name = 'TenantId'
                AND table_schema = 'smartinsight';";
                
        return migrationBuilder.Sql($@"
            DO $$
            DECLARE
                rec RECORD;
            BEGIN
                FOR rec IN {sql}
                LOOP
                    EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY;', rec.table_schema, rec.table_name);
                    EXECUTE format(
                        'DROP POLICY IF EXISTS %I_tenant_isolation_policy ON %I.%I;',
                        rec.table_name, rec.table_schema, rec.table_name
                    );
                    EXECUTE format(
                        'CREATE POLICY %I_tenant_isolation_policy ON %I.%I 
                         USING (tenant_id() IS NULL OR tenant_id() = ''00000000-0000-0000-0000-000000000000'' OR ""TenantId"" = tenant_id())
                         WITH CHECK (tenant_id() IS NULL OR tenant_id() = ''00000000-0000-0000-0000-000000000000'' OR ""TenantId"" = tenant_id());',
                        rec.table_name, rec.table_schema, rec.table_name
                    );
                END LOOP;
            END $$;");
    }
} 