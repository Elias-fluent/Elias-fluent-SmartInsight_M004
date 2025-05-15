using System;
using System.Threading.Tasks;
using SmartInsight.Tests.SQL.Common.Utilities;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Integration
{
    /// <summary>
    /// Base class for integration tests, providing database access and other integration-specific functionality
    /// </summary>
    public abstract class IntegrationTestBase : TestBase, IAsyncDisposable
    {
        /// <summary>
        /// Flag to track whether async dispose has been called
        /// </summary>
        private bool _disposed = false;
        
        /// <summary>
        /// Initializes a new instance of the IntegrationTestBase class
        /// </summary>
        /// <param name="outputHelper">XUnit test output helper for logging</param>
        protected IntegrationTestBase(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
            // Integration test specific setup
            InitializeIntegrationTestAsync().GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// Initialize async resources for integration tests
        /// </summary>
        private async Task InitializeIntegrationTestAsync()
        {
            LogInfo("Starting integration test setup...");
            
            try
            {
                // Initialize resources needed for integration tests
                // For example: database connections, test servers, etc.
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
        /// Asynchronously cleans up resources used by integration tests
        /// </summary>
        public async ValueTask DisposeAsync()
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