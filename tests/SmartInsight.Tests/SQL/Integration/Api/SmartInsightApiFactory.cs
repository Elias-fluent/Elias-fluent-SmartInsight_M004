using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartInsight.Tests.SQL.Integration.Containers;

namespace SmartInsight.Tests.SQL.Integration.Api
{
    /// <summary>
    /// A custom WebApplicationFactory for integration testing of the SmartInsight API
    /// </summary>
    public class SmartInsightApiFactory : WebApplicationFactory<object>, IAsyncDisposable
    {
        private readonly PostgresContainer _dbContainer;
        private readonly QdrantContainer _qdrantContainer;
        private bool _isDisposed;

        /// <summary>
        /// Gets the connection string to the test database
        /// </summary>
        public string DbConnectionString => _dbContainer.ConnectionString;

        /// <summary>
        /// Gets the URL to the Qdrant API
        /// </summary>
        public string QdrantApiUrl => _qdrantContainer.ApiUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartInsightApiFactory"/> class
        /// </summary>
        public SmartInsightApiFactory()
        {
            _dbContainer = new PostgresContainer();
            _qdrantContainer = new QdrantContainer();
        }

        /// <summary>
        /// Initializes the test environment asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();
            await _qdrantContainer.StartAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Use the assembly that contains the API
            builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Testing");
            
            // Uses the SmartInsight.API project for tests
            builder.UseStartup<TestStartup>();

            builder.ConfigureAppConfiguration(configBuilder =>
            {
                var config = new Dictionary<string, string>
                {
                    {"ConnectionStrings:DefaultConnection", DbConnectionString},
                    {"Qdrant:Endpoint", QdrantApiUrl},
                    {"Logging:LogLevel:Default", "Information"},
                    {"Logging:LogLevel:Microsoft", "Warning"},
                    {"Logging:LogLevel:Microsoft.Hosting.Lifetime", "Information"}
                };

                configBuilder.AddInMemoryCollection(config);
            });

            builder.ConfigureTestServices(services =>
            {
                // Replace services with test implementations if needed
            });
        }

        /// <summary>
        /// Disposes of the test environment
        /// </summary>
        public new async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                await base.DisposeAsync();
                await _dbContainer.DisposeAsync();
                await _qdrantContainer.DisposeAsync();
                _isDisposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
} 