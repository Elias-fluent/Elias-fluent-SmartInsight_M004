using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace SmartInsight.Tests.SQL.Integration.Containers
{
    /// <summary>
    /// A wrapper around the Testcontainers PostgreSQL container for integration tests
    /// </summary>
    public class PostgresContainer : IAsyncDisposable
    {
        private readonly PostgreSqlContainer _container;
        private readonly string _dbName;
        private readonly string _username;
        private readonly string _password;
        private bool _isDisposed;

        /// <summary>
        /// The connection string to the PostgreSQL container
        /// </summary>
        public string ConnectionString => _container.GetConnectionString();

        /// <summary>
        /// Creates a new instance of the PostgreSQL container
        /// </summary>
        /// <param name="dbName">The name of the database to create</param>
        /// <param name="username">The username to use</param>
        /// <param name="password">The password to use</param>
        public PostgresContainer(string dbName = "smartinsight_test", string username = "test_user", string password = "test_password")
        {
            _dbName = dbName;
            _username = username;
            _password = password;
            
            _container = new PostgreSqlBuilder()
                .WithDatabase(_dbName)
                .WithUsername(_username)
                .WithPassword(_password)
                .WithCleanUp(true)
                .WithImage("postgres:15")
                .WithPortBinding(Random.Shared.Next(5433, 5500), 5432)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
                .Build();
        }

        /// <summary>
        /// Starts the PostgreSQL container asynchronously
        /// </summary>
        public async Task StartAsync()
        {
            await _container.StartAsync();
        }

        /// <summary>
        /// Gets the host (including port) for the PostgreSQL container
        /// </summary>
        public string GetHost()
        {
            return $"{_container.Hostname}:{_container.GetMappedPublicPort(5432)}";
        }

        /// <summary>
        /// Gets the database name
        /// </summary>
        public string GetDatabaseName()
        {
            return _dbName;
        }

        /// <summary>
        /// Gets the username for the PostgreSQL container
        /// </summary>
        public string GetUsername()
        {
            return _username;
        }

        /// <summary>
        /// Gets the password for the PostgreSQL container
        /// </summary>
        public string GetPassword()
        {
            return _password;
        }

        /// <summary>
        /// Disposes of the PostgreSQL container
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                await _container.DisposeAsync();
                _isDisposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
} 