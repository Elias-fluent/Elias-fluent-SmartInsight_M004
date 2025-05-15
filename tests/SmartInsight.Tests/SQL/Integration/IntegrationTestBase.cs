using System;
using System.Net.Http;
using System.Threading.Tasks;
using SmartInsight.Tests.SQL.Common.Utilities;
using SmartInsight.Tests.SQL.Integration.Api;
using SmartInsight.Tests.SQL.Integration.Database;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Integration
{
    /// <summary>
    /// Base class for integration tests, providing database access and other integration-specific functionality
    /// </summary>
    public abstract class IntegrationTestBase : TestBase, IAsyncLifetime, IAsyncDisposable
    {
        /// <summary>
        /// The shared API factory for integration tests
        /// </summary>
        private static readonly SmartInsightApiFactory SharedApiFactory = new();

        /// <summary>
        /// The database reset helper
        /// </summary>
        private readonly DatabaseResetHelper _databaseResetHelper;

        /// <summary>
        /// The test data seeder
        /// </summary>
        private readonly TestDataSeeder _testDataSeeder;

        /// <summary>
        /// Flag to track whether async dispose has been called
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Gets the API client for making HTTP requests to the test API
        /// </summary>
        protected HttpClient ApiClient { get; private set; }

        /// <summary>
        /// Gets the connection string to the test database
        /// </summary>
        protected string DbConnectionString => SharedApiFactory.DbConnectionString;

        /// <summary>
        /// Gets the URL to the Qdrant API
        /// </summary>
        protected string QdrantApiUrl => SharedApiFactory.QdrantApiUrl;

        /// <summary>
        /// Gets the default test tenant ID
        /// </summary>
        protected static Guid DefaultTestTenantId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

        /// <summary>
        /// Initializes a new instance of the IntegrationTestBase class
        /// </summary>
        /// <param name="outputHelper">XUnit test output helper for logging</param>
        protected IntegrationTestBase(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
            _databaseResetHelper = new DatabaseResetHelper(DbConnectionString);
            _testDataSeeder = new TestDataSeeder(DbConnectionString);
            ApiClient = SharedApiFactory.CreateClient();
        }

        /// <summary>
        /// Initialize async resources for integration tests
        /// This is called by xUnit before each test
        /// </summary>
        public async Task InitializeAsync()
        {
            LogInfo("Starting integration test setup...");
            
            try
            {
                // Ensure API factory is initialized
                if (ApiClient == null)
                {
                    await InitializeApiFactoryAsync();
                    ApiClient = SharedApiFactory.CreateClient();
                }

                // Reset database to clean state
                await _databaseResetHelper.ResetAsync();
                
                // Initialize additional resources needed for integration tests
                await SetupIntegrationTestAsync();
                
                LogInfo("Integration test setup completed successfully");
            }
            catch (Exception ex)
            {
                LogError("Integration test setup failed", ex);
                throw;
            }
        }

        /// <summary>
        /// Initialize the API factory asynchronously if it hasn't been initialized yet
        /// This is a static operation that should only happen once for all tests
        /// </summary>
        private static async Task InitializeApiFactoryAsync()
        {
            // Use a static lock if needed to prevent race conditions
            await SharedApiFactory.InitializeAsync();
        }
        
        /// <summary>
        /// Setup integration test resources
        /// Override this method to initialize resources needed for integration tests
        /// </summary>
        protected virtual async Task SetupIntegrationTestAsync()
        {
            // Base implementation does nothing
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Cleanup integration test resources
        /// Override this method to clean up resources used by integration tests
        /// </summary>
        protected virtual async Task CleanupIntegrationTestAsync()
        {
            // Base implementation does nothing
            await Task.CompletedTask;
        }

        /// <summary>
        /// Seeds the database with basic test data using the default test tenant ID
        /// </summary>
        protected async Task SeedBasicTestDataAsync()
        {
            await _testDataSeeder.SeedBasicDataAsync(DefaultTestTenantId);
            LogInfo($"Seeded basic test data for tenant {DefaultTestTenantId}");
        }

        /// <summary>
        /// Seeds the database with basic test data for a specific tenant
        /// </summary>
        /// <param name="tenantId">The tenant ID to use</param>
        protected async Task SeedBasicTestDataAsync(Guid tenantId)
        {
            await _testDataSeeder.SeedBasicDataAsync(tenantId);
            LogInfo($"Seeded basic test data for tenant {tenantId}");
        }

        /// <summary>
        /// Executes a custom SQL script for test-specific data seeding
        /// </summary>
        /// <param name="sqlScript">The SQL script to execute</param>
        /// <param name="parameters">Optional parameters for the SQL script</param>
        protected async Task ExecuteCustomSqlAsync(string sqlScript, System.Collections.Generic.Dictionary<string, object> parameters = null)
        {
            await _testDataSeeder.ExecuteCustomSqlAsync(sqlScript, parameters);
            LogInfo("Executed custom SQL script for test data");
        }
        
        /// <summary>
        /// Cleanup called by xUnit after each test (IAsyncLifetime implementation)
        /// </summary>
        async Task IAsyncLifetime.DisposeAsync()
        {
            await CleanupAsync();
        }

        /// <summary>
        /// Cleanup resources (IAsyncDisposable implementation)
        /// </summary>
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await CleanupAsync();
        }

        /// <summary>
        /// Common cleanup logic used by both DisposeAsync implementations
        /// </summary>
        private async Task CleanupAsync()
        {
            if (!_disposed)
            {
                LogInfo("Cleaning up integration test resources...");
                
                try
                {
                    await CleanupIntegrationTestAsync();
                }
                catch (Exception ex)
                {
                    LogError("Error during integration test cleanup", ex);
                    throw;
                }
                finally
                {
                    _disposed = true;
                    
                    // Don't dispose the shared API factory here, 
                    // it will be disposed when the test class is disposed
                    
                    // Call the synchronous Dispose
                    Dispose();
                }
            }
        }
        
        /// <summary>
        /// Synchronous cleanup implementation
        /// </summary>
        public override void Dispose()
        {
            if (!_disposed)
            {
                // Perform synchronous cleanup here
                
                base.Dispose();
                
                _disposed = true;
            }
        }
    }
} 