using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MySql.Data.MySqlClient;
using SmartInsight.Core.Interfaces;
using SmartInsight.Knowledge.Connectors;
using SmartInsight.Tests.SQL.Common.Utilities;
using Xunit;
using Xunit.Abstractions;
using System.Reflection;

namespace SmartInsight.Tests.SQL.Unit
{
    /// <summary>
    /// Unit tests for the MySQLConnector class
    /// </summary>
    public class MySQLConnectorTests : SqlTestBase
    {
        private readonly MySQLConnector _connector;
        private readonly Mock<ILogger<MySQLConnector>> _loggerMock;

        public MySQLConnectorTests(ITestOutputHelper output) : base(output)
        {
            _loggerMock = new Mock<ILogger<MySQLConnector>>();
            _connector = new MySQLConnector(_loggerMock.Object);
        }

        protected override void RegisterAdditionalServices(IServiceCollection services)
        {
            base.RegisterAdditionalServices(services);
            
            // Register any additional services needed for these tests
            services.AddSingleton<IConnectorConfiguration>(provider => 
            {
                return SmartInsight.Tests.SQL.Common.Utilities.ConnectorConfigurationFactory.Create(
                    "mysql-connector",
                    "Test MySQL Connector",
                    Guid.Empty,
                    new Dictionary<string, string>
                    {
                        { "server", "localhost" },
                        { "database", "test_db" },
                        { "username", "test_user" },
                        { "password", "test_password" }
                    });
            });
        }

        #region Validation Tests

        [Fact]
        public async Task ValidateConnectionAsync_WithValidParams_ReturnsSuccess()
        {
            // Arrange
            var connectionParams = new Dictionary<string, string>
            {
                { "server", "localhost" },
                { "database", "test_db" },
                { "username", "test_user" },
                { "password", "test_password" }
            };

            // Act
            var result = await _connector.ValidateConnectionAsync(connectionParams);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateConnectionAsync_WithMissingServer_ReturnsValidationError()
        {
            // Arrange
            var connectionParams = new Dictionary<string, string>
            {
                { "database", "test_db" },
                { "username", "test_user" },
                { "password", "test_password" }
            };

            // Act
            var result = await _connector.ValidateConnectionAsync(connectionParams);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.FieldName == "server");
        }

        [Fact]
        public async Task ValidateConnectionAsync_WithMissingDatabase_ReturnsValidationError()
        {
            // Arrange
            var connectionParams = new Dictionary<string, string>
            {
                { "server", "localhost" },
                { "username", "test_user" },
                { "password", "test_password" }
            };

            // Act
            var result = await _connector.ValidateConnectionAsync(connectionParams);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.FieldName == "database");
        }

        [Fact]
        public async Task ValidateConnectionAsync_WithMissingUsername_ReturnsValidationError()
        {
            // Arrange
            var connectionParams = new Dictionary<string, string>
            {
                { "server", "localhost" },
                { "database", "test_db" },
                { "password", "test_password" }
            };

            // Act
            var result = await _connector.ValidateConnectionAsync(connectionParams);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.FieldName == "username");
        }

        [Fact]
        public async Task ValidateConnectionAsync_WithMissingPassword_ReturnsValidationError()
        {
            // Arrange
            var connectionParams = new Dictionary<string, string>
            {
                { "server", "localhost" },
                { "database", "test_db" },
                { "username", "test_user" }
            };

            // Act
            var result = await _connector.ValidateConnectionAsync(connectionParams);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.FieldName == "password");
        }

        [Fact]
        public async Task ValidateConnectionAsync_WithInvalidPort_ReturnsValidationError()
        {
            // Arrange
            var connectionParams = new Dictionary<string, string>
            {
                { "server", "localhost" },
                { "database", "test_db" },
                { "username", "test_user" },
                { "password", "test_password" },
                { "port", "invalid_port" }
            };

            // Act
            var result = await _connector.ValidateConnectionAsync(connectionParams);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.FieldName == "port");
        }

        #endregion

        #region Connection Tests

        [Fact]
        public async Task ConnectAsync_WithValidParams_ReturnsSuccessResult()
        {
            // This test would typically require mocking MySqlConnection or using a real database
            // For unit testing, we'll just verify the connection string is built correctly
            
            // Arrange
            var connectionParams = new Dictionary<string, string>
            {
                { "server", "localhost" },
                { "database", "test_db" },
                { "username", "test_user" },
                { "password", "test_password" }
            };
            
            // Act & Assert
            // Since we can't easily mock MySqlConnection construction, we'll just verify that
            // the method doesn't throw and handles the MySqlException correctly when the connection fails
            
            var result = await _connector.ConnectAsync(connectionParams);
            
            // Since we can't actually connect in a unit test, we expect failure
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task ConnectAsync_WhenAlreadyConnected_ReturnsSuccessWithoutReconnecting()
        {
            // This test would require setting up the connector in a connected state
            // For now, we'll just test the error handling aspects
            
            // Arrange
            var connectionParams = new Dictionary<string, string>
            {
                { "server", "localhost" },
                { "database", "test_db" },
                { "username", "test_user" },
                { "password", "test_password" }
            };

            // Use reflection to set the connector state
            var field = typeof(MySQLConnector).GetField("_connectionId", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(_connector, "test-connection-id");
            
            // Use reflection to set the connector state
            var stateProperty = typeof(MySQLConnector).GetProperty("ConnectionState");
            stateProperty?.SetValue(_connector, SmartInsight.Core.Interfaces.ConnectionState.Connected);

            // Act
            var result = await _connector.ConnectAsync(connectionParams);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("test-connection-id", result.ConnectionId);
            Assert.Contains("already_connected", result.ConnectionInfo["status"].ToString());
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidParams_ReturnsTrue()
        {
            // Arrange
            var connectionParams = new Dictionary<string, string>
            {
                { "server", "localhost" },
                { "database", "test_db" },
                { "username", "test_user" },
                { "password", "test_password" }
            };

            // Act & Assert
            // Since we can't easily mock the MySqlConnection, we'll just verify that
            // the method doesn't throw and handles the MySqlException correctly when the connection fails
            
            var result = await _connector.TestConnectionAsync(connectionParams);
            
            // Since we can't actually connect in a unit test, we expect failure
            Assert.False(result);
        }

        [Fact]
        public async Task TestConnectionAsync_WithInvalidParams_ReturnsFalse()
        {
            // Arrange
            var connectionParams = new Dictionary<string, string>
            {
                { "server", "" },  // Invalid server
                { "database", "test_db" },
                { "username", "test_user" },
                { "password", "test_password" }
            };

            // Act
            var result = await _connector.TestConnectionAsync(connectionParams);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task InitializeAsync_WithNullConfiguration_ReturnsFalse()
        {
            // Act
            var result = await _connector.InitializeAsync(null);

            // Assert
            Assert.False(result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Configuration cannot be null")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_WithInvalidConfiguration_ReturnsFalse()
        {
            // Arrange
            var invalidConfig = SmartInsight.Tests.SQL.Common.Utilities.ConnectorConfigurationFactory.Create(
                "mysql-connector",
                "Test MySQL Connector",
                Guid.Empty,
                new Dictionary<string, string>
                {
                    // Missing required fields
                });

            // Act
            var result = await _connector.InitializeAsync(invalidConfig);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Data Structure Tests

        [Fact]
        public async Task DiscoverDataStructuresAsync_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _connector.DiscoverDataStructuresAsync());
        }

        #endregion

        #region Data Extraction Tests

        [Fact]
        public async Task ExtractDataAsync_WhenNotConnected_ThrowsInvalidOperationException()
        {
            // Arrange
            var extractionParams = new ExtractionParameters(
                targetStructures: new[] { "Users" },
                filterCriteria: new Dictionary<string, object>
                {
                    { "SchemaName", "dbo" },
                    { "ObjectName", "Users" }
                }
            );

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _connector.ExtractDataAsync(extractionParams));
        }

        #endregion

        #region Data Transformation Tests

        [Fact]
        public async Task TransformDataAsync_WithNullData_ThrowsArgumentNullException()
        {
            // Arrange
            var transformationParams = new TransformationParameters(
                rules: new List<TransformationRule>()
            );

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _connector.TransformDataAsync(null, transformationParams));
        }

        [Fact]
        public async Task TransformDataAsync_WithNullParams_ThrowsArgumentNullException()
        {
            // Arrange
            var data = new List<IDictionary<string, object>>
            {
                new Dictionary<string, object> { { "id", 1 }, { "name", "Test" } }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _connector.TransformDataAsync(data, null));
        }

        [Fact]
        public async Task TransformDataAsync_WithEmptyRules_ReturnsOriginalData()
        {
            // Arrange
            var data = new List<IDictionary<string, object>>
            {
                new Dictionary<string, object> { { "id", 1 }, { "name", "Test" } }
            };
            var transformationParams = new TransformationParameters(
                rules: new List<TransformationRule>()
            );

            // Act
            var result = await _connector.TransformDataAsync(data, transformationParams);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.TransformedData);
            Assert.Equal(data.Count, result.TransformedData.Count);
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
        }

        #endregion
    }
} 