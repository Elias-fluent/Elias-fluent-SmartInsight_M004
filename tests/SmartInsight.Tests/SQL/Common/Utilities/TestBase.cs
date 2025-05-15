using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Common.Utilities
{
    /// <summary>
    /// Base class for all SmartInsight tests, providing common functionality
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected readonly ITestOutputHelper _outputHelper;
        
        /// <summary>
        /// Initializes a new instance of the TestBase class
        /// </summary>
        /// <param name="outputHelper">XUnit test output helper for logging to test console</param>
        protected TestBase(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            
            // Perform any common setup for all tests
            Setup();
        }
        
        /// <summary>
        /// Performs common setup operations for all tests
        /// </summary>
        protected virtual void Setup()
        {
            // Base implementation does nothing
            // Derived classes can override to add specific setup logic
        }
        
        /// <summary>
        /// Log message to the test output
        /// </summary>
        /// <param name="message">Message to log</param>
        protected void LogInfo(string message)
        {
            _outputHelper.WriteLine($"[INFO] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} - {message}");
        }
        
        /// <summary>
        /// Log error message to the test output
        /// </summary>
        /// <param name="message">Error message to log</param>
        /// <param name="exception">Optional exception</param>
        protected void LogError(string message, Exception? exception = null)
        {
            _outputHelper.WriteLine($"[ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} - {message}");
            
            if (exception != null)
            {
                _outputHelper.WriteLine($"Exception: {exception.GetType().Name}");
                _outputHelper.WriteLine($"Message: {exception.Message}");
                _outputHelper.WriteLine($"StackTrace: {exception.StackTrace}");
            }
        }
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public virtual void Dispose()
        {
            // Base implementation does nothing
            // Derived classes should override to clean up resources
            GC.SuppressFinalize(this);
        }
    }
} 