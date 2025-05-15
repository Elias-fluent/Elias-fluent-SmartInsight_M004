using System;
using System.Threading.Tasks;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Integration.Database
{
    /// <summary>
    /// Example integration tests for database operations
    /// </summary>
    public class DatabaseIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseIntegrationTests"/> class
        /// </summary>
        /// <param name="outputHelper">The test output helper</param>
        public DatabaseIntegrationTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        /// <summary>
        /// Setup for each test - override for custom setup
        /// </summary>
        protected override async Task SetupIntegrationTestAsync()
        {
            // Seed the database with common test data
            await SeedBasicTestDataAsync();
        }

        /// <summary>
        /// Example test demonstrating database seeding and querying
        /// </summary>
        [Fact]
        public async Task SeededTenant_CanBeQueried()
        {
            // Arrange
            const string sql = "SELECT name FROM Tenants WHERE Id = @Id";

            // Act
            string tenantName = null;
            await using (var connection = new NpgsqlConnection(DbConnectionString))
            {
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("Id", DefaultTestTenantId);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    tenantName = reader.GetString(0);
                }
            }

            // Assert
            Assert.NotNull(tenantName);
            Assert.Equal("Test Tenant", tenantName);
            LogInfo($"Successfully queried tenant: {tenantName}");
        }

        /// <summary>
        /// Example test demonstrating custom SQL execution
        /// </summary>
        [Fact]
        public async Task CustomSql_CanBeExecuted()
        {
            // Arrange
            var customId = Guid.NewGuid();
            const string insertSql = @"
                INSERT INTO Tenants (Id, Name, CreatedAt, IsActive) 
                VALUES (@Id, @Name, @CreatedAt, @IsActive)";
            
            var parameters = new System.Collections.Generic.Dictionary<string, object>
            {
                { "Id", customId },
                { "Name", "Custom Tenant" },
                { "CreatedAt", DateTime.UtcNow },
                { "IsActive", true }
            };

            // Act
            await ExecuteCustomSqlAsync(insertSql, parameters);

            // Now retrieve the tenant
            string tenantName = null;
            await using (var connection = new NpgsqlConnection(DbConnectionString))
            {
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand("SELECT name FROM Tenants WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("Id", customId);

                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    tenantName = reader.GetString(0);
                }
            }

            // Assert
            Assert.NotNull(tenantName);
            Assert.Equal("Custom Tenant", tenantName);
            LogInfo($"Successfully created and queried custom tenant: {tenantName}");
        }

        /// <summary>
        /// Example test demonstrating multiple tenant operations
        /// </summary>
        [Fact]
        public async Task MultipleTenants_AreIsolated()
        {
            // Arrange
            var tenant1Id = Guid.NewGuid();
            var tenant2Id = Guid.NewGuid();

            // Seed data for two different tenants
            await SeedBasicTestDataAsync(tenant1Id);
            await SeedBasicTestDataAsync(tenant2Id);

            // Add custom data for each tenant
            const string customSql = @"
                INSERT INTO Users (Id, TenantId, Email, Username, CreatedAt, IsActive)
                VALUES (@Id, @TenantId, @Email, @Username, @CreatedAt, @IsActive)";

            var user1Params = new System.Collections.Generic.Dictionary<string, object>
            {
                { "Id", Guid.NewGuid() },
                { "TenantId", tenant1Id },
                { "Email", "tenant1@example.com" },
                { "Username", "Tenant 1 User" },
                { "CreatedAt", DateTime.UtcNow },
                { "IsActive", true }
            };

            var user2Params = new System.Collections.Generic.Dictionary<string, object>
            {
                { "Id", Guid.NewGuid() },
                { "TenantId", tenant2Id },
                { "Email", "tenant2@example.com" },
                { "Username", "Tenant 2 User" },
                { "CreatedAt", DateTime.UtcNow },
                { "IsActive", true }
            };

            await ExecuteCustomSqlAsync(customSql, user1Params);
            await ExecuteCustomSqlAsync(customSql, user2Params);

            // Act & Assert
            // Verify each tenant has the correct users
            await using (var connection = new NpgsqlConnection(DbConnectionString))
            {
                await connection.OpenAsync();
                
                // Count users for tenant 1
                await using (var command = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE TenantId = @TenantId", connection))
                {
                    command.Parameters.AddWithValue("TenantId", tenant1Id);
                    var tenant1Count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    
                    // Should have at least the one user we added
                    Assert.True(tenant1Count >= 1);
                    LogInfo($"Tenant 1 has {tenant1Count} users");
                }
                
                // Count users for tenant 2
                await using (var command = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE TenantId = @TenantId", connection))
                {
                    command.Parameters.AddWithValue("TenantId", tenant2Id);
                    var tenant2Count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    
                    // Should have at least the one user we added
                    Assert.True(tenant2Count >= 1);
                    LogInfo($"Tenant 2 has {tenant2Count} users");
                }
            }
        }
    }
} 