using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartInsight.AI.SQL;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Models;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Common.Utilities
{
    /// <summary>
    /// Base class for SQL-related tests providing common setup and utilities
    /// </summary>
    public abstract class SqlTestBase : IDisposable
    {
        protected readonly ITestOutputHelper _output;
        protected readonly ServiceProvider _serviceProvider;
        protected readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of the test base with common services registered
        /// </summary>
        /// <param name="output">XUnit test output helper for logging</param>
        protected SqlTestBase(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            
            // Create a service collection with test configuration
            var services = new ServiceCollection();
            
            // Configure logging to use XUnit test output
            services.AddLogging(builder => 
            {
                builder.AddProvider(new XUnitLoggerProvider(_output));
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            // Register SQL services
            services.AddSqlParameterValidation();
            services.AddSqlInjectionPrevention();
            services.AddSqlValidationRulesEngine();
            services.AddQueryOptimization();
            services.AddSqlLogging();
            services.AddSqlLogRetention();
            
            // Allow inheriting classes to register additional services
            RegisterAdditionalServices(services);
            
            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();
            
            // Get the logger
            _logger = _serviceProvider.GetRequiredService<ILogger<SqlTestBase>>();
        }
        
        /// <summary>
        /// Override this method to register additional services
        /// </summary>
        /// <param name="services">The service collection</param>
        protected virtual void RegisterAdditionalServices(IServiceCollection services)
        {
            // Default implementation does nothing
        }
        
        /// <summary>
        /// Creates a sample SQL template for testing
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="name">Template name</param>
        /// <param name="sql">SQL template text</param>
        /// <returns>SqlTemplate instance</returns>
        protected SqlTemplate CreateSampleTemplate(string id, string name, string sql)
        {
            return new SqlTemplate
            {
                Id = id,
                Name = name,
                Description = $"Test template {name}",
                SqlTemplateText = sql,
                Parameters = new List<SqlTemplateParameter>
                {
                    new SqlTemplateParameter
                    {
                        Name = "testParam",
                        Type = "String",
                        Required = true,
                        Description = "Test parameter"
                    }
                },
                Created = DateTime.UtcNow,
                Version = "1.0",
                IntentMapping = new List<string> { "TestIntent" },
                Tags = new List<string> { "Test" }
            };
        }
        
        /// <summary>
        /// Creates a new tenant context for testing
        /// </summary>
        /// <returns>TenantContext instance</returns>
        protected TenantContext CreateTestTenantContext()
        {
            return new TenantContext
            {
                TenantId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Permissions = new List<string> { "ReadData", "WriteData" }
            };
        }

        /// <summary>
        /// Creates a dictionary of parameters for testing
        /// </summary>
        /// <returns>Dictionary of parameter values</returns>
        protected Dictionary<string, object> CreateTestParameters()
        {
            return new Dictionary<string, object>
            {
                { "testParam", "TestValue" },
                { "numParam", 42 },
                { "dateParam", DateTime.UtcNow }
            };
        }

        /// <summary>
        /// Creates a dictionary of parameters with potential security issues for negative testing
        /// </summary>
        /// <returns>Dictionary of parameter values</returns>
        protected Dictionary<string, object> CreateUnsafeParameters()
        {
            return new Dictionary<string, object>
            {
                { "testParam", "'; DROP TABLE Users; --" },
                { "numParam", 999999999 },
                { "dateParam", DateTime.MaxValue }
            };
        }

        /// <summary>
        /// Creates a safe SQL query for testing
        /// </summary>
        /// <returns>Safe SQL query string</returns>
        protected string CreateSafeSqlQuery()
        {
            return "SELECT Id, Name, Email FROM Users WHERE DepartmentId = @departmentId";
        }

        /// <summary>
        /// Creates an unsafe SQL query for testing
        /// </summary>
        /// <returns>Unsafe SQL query string</returns>
        protected string CreateUnsafeSqlQuery()
        {
            return "SELECT * FROM Users; DROP TABLE Logs; --";
        }
        
        /// <summary>
        /// Disposes of the service provider
        /// </summary>
        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
    
    /// <summary>
    /// XUnit logger provider for capturing test output
    /// </summary>
    internal class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_testOutputHelper, categoryName);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// XUnit logger implementation
    /// </summary>
    internal class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;

        public XUnitLogger(ITestOutputHelper testOutputHelper, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return DummyDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                _testOutputHelper.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{logLevel}] {_categoryName}: {formatter(state, exception)}");

                if (exception != null)
                {
                    _testOutputHelper.WriteLine($"Exception: {exception}");
                }
            }
            catch
            {
                // Ignore failures (usually from tests completing before log is written)
            }
        }
    }

    /// <summary>
    /// Dummy disposable for logger scopes
    /// </summary>
    internal class DummyDisposable : IDisposable
    {
        public static readonly DummyDisposable Instance = new DummyDisposable();

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
} 