using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using System.Data;

namespace SmartInsight.Tests.SQL.Integration.Database
{
    /// <summary>
    /// Helper class for seeding test data in the database for integration tests
    /// </summary>
    public class TestDataSeeder
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDataSeeder"/> class
        /// </summary>
        /// <param name="connectionString">The database connection string</param>
        public TestDataSeeder(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Seeds the database with basic test data
        /// </summary>
        /// <param name="tenantId">The tenant ID to use for the test data</param>
        public async Task SeedBasicDataAsync(Guid tenantId)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Seed test tenant if it doesn't exist
            await SeedTestTenantAsync(connection, tenantId);

            // Additional data seeding methods would be called here based on test needs
            // For example:
            // await SeedUsersAsync(connection, tenantId);
            // await SeedDataSourcesAsync(connection, tenantId);
        }

        /// <summary>
        /// Seeds a test tenant record if it doesn't exist
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="tenantId">The tenant ID</param>
        private async Task SeedTestTenantAsync(NpgsqlConnection connection, Guid tenantId)
        {
            const string sql = @"
                INSERT INTO Tenants (Id, Name, CreatedAt, IsActive)
                VALUES (@Id, @Name, @CreatedAt, @IsActive)
                ON CONFLICT (Id) DO NOTHING;";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("Id", tenantId);
            command.Parameters.AddWithValue("Name", "Test Tenant");
            command.Parameters.AddWithValue("CreatedAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("IsActive", true);

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Seeds test users for a tenant
        /// </summary>
        /// <param name="connection">The database connection</param>
        /// <param name="tenantId">The tenant ID</param>
        public async Task SeedUsersAsync(NpgsqlConnection connection, Guid tenantId)
        {
            const string sql = @"
                INSERT INTO Users (Id, TenantId, Email, Username, CreatedAt, IsActive)
                VALUES (@Id, @TenantId, @Email, @Username, @CreatedAt, @IsActive)
                ON CONFLICT (Email) DO NOTHING;";

            // Create a few test users
            var testUsers = new[]
            {
                new { Id = Guid.NewGuid(), Email = "admin@test.com", Username = "Admin User" },
                new { Id = Guid.NewGuid(), Email = "user1@test.com", Username = "Test User 1" },
                new { Id = Guid.NewGuid(), Email = "user2@test.com", Username = "Test User 2" }
            };

            foreach (var user in testUsers)
            {
                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("Id", user.Id);
                command.Parameters.AddWithValue("TenantId", tenantId);
                command.Parameters.AddWithValue("Email", user.Email);
                command.Parameters.AddWithValue("Username", user.Username);
                command.Parameters.AddWithValue("CreatedAt", DateTime.UtcNow);
                command.Parameters.AddWithValue("IsActive", true);

                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Executes a custom SQL script for seeding test-specific data
        /// </summary>
        /// <param name="sqlScript">The SQL script to execute</param>
        /// <param name="parameters">Optional parameters for the SQL script</param>
        public async Task ExecuteCustomSqlAsync(string sqlScript, Dictionary<string, object> parameters = null)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sqlScript, connection);
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }

            await command.ExecuteNonQueryAsync();
        }
    }
} 