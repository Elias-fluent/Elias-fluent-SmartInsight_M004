using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Respawn;

namespace SmartInsight.Tests.SQL.Integration.Database
{
    /// <summary>
    /// Helper class for resetting the database to a clean state between tests
    /// </summary>
    public class DatabaseResetHelper
    {
        private readonly string _connectionString;
        private Respawner _respawner;
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseResetHelper"/> class
        /// </summary>
        /// <param name="connectionString">The database connection string</param>
        public DatabaseResetHelper(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Initializes the respawner
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                TablesToIgnore = new[] 
                {
                    new Respawn.Graph.Table("__EFMigrationsHistory")
                }
            });

            _initialized = true;
        }

        /// <summary>
        /// Resets the database to a clean state
        /// </summary>
        public async Task ResetAsync()
        {
            if (!_initialized)
                await InitializeAsync();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }
    }
} 