using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using SmartInsight.Core.Interfaces;
using SmartInsight.Knowledge.Connectors;
using SmartInsight.Tests.SQL.Common.Utilities;
using Testcontainers.MySql;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Integration
{
    /// <summary>
    /// Integration tests for the MySQLConnector class
    /// These tests require a MySQL Docker container to be running
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("RequiresSetup", "MySQL")]
    public class MySQLConnectorIntegrationTests : TestBase, IAsyncLifetime
    {
        private readonly MySQLConnector _connector;
        private readonly string _testDbName = "smartinsight_test_db";
        private MySqlContainer _mySqlContainer;
        private Dictionary<string, string> _validConnectionParams;
        private readonly ServiceProvider _serviceProvider;
        private const int MYSQL_DEFAULT_PORT = 3306;

        public MySQLConnectorIntegrationTests(ITestOutputHelper output) 
            : base(output)
        {
            // Create a service collection with needed services
            var services = new ServiceCollection();
            
            // Configure logging to use XUnit test output
            services.AddLogging(builder => 
            {
                builder.AddProvider(new XUnitLoggerProvider(output));
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            _serviceProvider = services.BuildServiceProvider();
            
            _connector = new MySQLConnector(
                _serviceProvider.GetRequiredService<ILogger<MySQLConnector>>());

            // Setup MySQL container
            _mySqlContainer = new MySqlBuilder()
                .WithImage("mysql:8.0")
                .WithPassword("Password123!")
                .WithUsername("root")
                .WithDatabase(_testDbName)
                .Build();
        }

        public async Task InitializeAsync()
        {
            LogInfo("Starting MySQL container for integration tests");
            await _mySqlContainer.StartAsync();
            
            // Setup connection parameters
            _validConnectionParams = new Dictionary<string, string>
            {
                { "server", _mySqlContainer.Hostname },
                { "port", _mySqlContainer.GetMappedPublicPort(MYSQL_DEFAULT_PORT).ToString() },
                { "database", _testDbName },
                { "username", "root" },
                { "password", "Password123!" },
                { "sslMode", "none" }
            };
            
            LogInfo($"MySQL container started at {_validConnectionParams["server"]}:{_validConnectionParams["port"]}");
            
            try
            {
                // Create test schema and sample data
                await CreateTestSchemaAsync();
                
                LogInfo("Test schema setup completed successfully");
            }
            catch (Exception ex)
            {
                LogError("Failed to set up test schema", ex);
                throw;
            }
        }

        public async Task DisposeAsync()
        {
            if (_mySqlContainer != null)
            {
                LogInfo("Stopping MySQL container");
                await _mySqlContainer.DisposeAsync();
            }
        }

        /// <summary>
        /// Creates test schema and sample data for integration tests
        /// </summary>
        private async Task CreateTestSchemaAsync()
        {
            LogInfo($"Connecting to test database '{_testDbName}'");
            
            var connectionString = _mySqlContainer.GetConnectionString();
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Create Customers table
            LogInfo("Creating Customers table");
            string createCustomersTable = @"
                CREATE TABLE IF NOT EXISTS Customers (
                    CustomerId INT AUTO_INCREMENT PRIMARY KEY,
                    FirstName VARCHAR(50) NOT NULL,
                    LastName VARCHAR(50) NOT NULL,
                    Email VARCHAR(100) NOT NULL UNIQUE,
                    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    IsActive BOOLEAN NOT NULL DEFAULT TRUE
                )";
            await using (var cmd = new MySqlCommand(createCustomersTable, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Create Orders table
            LogInfo("Creating Orders table");
            string createOrdersTable = @"
                CREATE TABLE IF NOT EXISTS Orders (
                    OrderId INT AUTO_INCREMENT PRIMARY KEY,
                    CustomerId INT NOT NULL,
                    OrderDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    TotalAmount DECIMAL(10, 2) NOT NULL,
                    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
                    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
                )";
            await using (var cmd = new MySqlCommand(createOrdersTable, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Create OrderItems table
            LogInfo("Creating OrderItems table");
            string createOrderItemsTable = @"
                CREATE TABLE IF NOT EXISTS OrderItems (
                    OrderItemId INT AUTO_INCREMENT PRIMARY KEY,
                    OrderId INT NOT NULL,
                    ProductName VARCHAR(100) NOT NULL,
                    Quantity INT NOT NULL,
                    UnitPrice DECIMAL(10, 2) NOT NULL,
                    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
                )";
            await using (var cmd = new MySqlCommand(createOrderItemsTable, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Insert sample data into Customers
            LogInfo("Inserting sample data into Customers table");
            string insertCustomers = @"
                INSERT INTO Customers (FirstName, LastName, Email, IsActive) VALUES
                ('John', 'Doe', 'john.doe@example.com', TRUE),
                ('Jane', 'Smith', 'jane.smith@example.com', TRUE),
                ('Mike', 'Johnson', 'mike.johnson@example.com', FALSE)";
            await using (var cmd = new MySqlCommand(insertCustomers, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Insert sample data into Orders
            LogInfo("Inserting sample data into Orders table");
            string insertOrders = @"
                INSERT INTO Orders (CustomerId, OrderDate, TotalAmount, Status) VALUES
                (1, '2023-01-15', 125.50, 'Completed'),
                (1, '2023-02-20', 75.25, 'Shipped'),
                (2, '2023-03-10', 200.00, 'Pending')";
            await using (var cmd = new MySqlCommand(insertOrders, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Insert sample data into OrderItems
            LogInfo("Inserting sample data into OrderItems table");
            string insertOrderItems = @"
                INSERT INTO OrderItems (OrderId, ProductName, Quantity, UnitPrice) VALUES
                (1, 'Widget A', 2, 50.00),
                (1, 'Widget B', 1, 25.50),
                (2, 'Widget C', 3, 25.00),
                (3, 'Widget D', 4, 50.00)";
            await using (var cmd = new MySqlCommand(insertOrderItems, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        #region Connection Tests

        [Fact]
        public async Task ValidateConnectionAsync_WithValidParams_ReturnsSuccess()
        {
            // Act
            var result = await _connector.ValidateConnectionAsync(_validConnectionParams);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateConnectionAsync_WithInvalidServer_ReturnsValidationSuccess()
        {
            // Arrange - Just changing server name doesn't fail validation because it's just checking parameters
            var connectionParams = new Dictionary<string, string>(_validConnectionParams);
            connectionParams["server"] = "invalid-server";

            // Act
            var result = await _connector.ValidateConnectionAsync(connectionParams);

            // Assert - this will still pass validation because we only validate structure not connectivity
            Assert.NotNull(result);
            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task ConnectAsync_WithValidParams_ReturnsSuccessResult()
        {
            // Act
            var result = await _connector.ConnectAsync(_validConnectionParams);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.ConnectionId);
            Assert.NotNull(result.ServerVersion);
            Assert.NotNull(result.ConnectionInfo);

            // Clean up
            await _connector.DisconnectAsync();
        }

        [Fact]
        public async Task ConnectAsync_WithInvalidServerName_ReturnsFailureResult()
        {
            // Arrange
            var invalidParams = new Dictionary<string, string>(_validConnectionParams);
            invalidParams["server"] = "non-existent-server";

            // Act
            var result = await _connector.ConnectAsync(invalidParams);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidParams_ReturnsTrue()
        {
            // Act
            var result = await _connector.TestConnectionAsync(_validConnectionParams);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TestConnectionAsync_WithInvalidServerName_ReturnsFalse()
        {
            // Arrange
            var invalidParams = new Dictionary<string, string>(_validConnectionParams);
            invalidParams["server"] = "non-existent-server";

            // Act
            var result = await _connector.TestConnectionAsync(invalidParams);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Data Structure Tests

        [Fact]
        public async Task DiscoverDataStructuresAsync_ReturnsAllTables()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);

            // Act
            var structures = await _connector.DiscoverDataStructuresAsync();

            // Assert
            Assert.NotNull(structures);
            Assert.Contains(structures, s => s.Name.Equals("Customers", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(structures, s => s.Name.Equals("Orders", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(structures, s => s.Name.Equals("OrderItems", StringComparison.OrdinalIgnoreCase));

            // Clean up
            await _connector.DisconnectAsync();
        }

        [Fact]
        public async Task DiscoverDataStructuresAsync_WithFilteredSchema_ReturnsFilteredTables()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);
            var filter = new Dictionary<string, object>
            {
                { "schema", _testDbName }
            };

            // Act
            var structures = await _connector.DiscoverDataStructuresAsync(filter);

            // Assert
            Assert.NotNull(structures);
            Assert.Contains(structures, s => s.Name.Equals("Customers", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(structures, s => s.Name.Equals("Orders", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(structures, s => s.Name.Equals("OrderItems", StringComparison.OrdinalIgnoreCase));

            // Clean up
            await _connector.DisconnectAsync();
        }

        #endregion

        #region Data Extraction Tests

        [Fact]
        public async Task ExtractDataAsync_SimpleQuery_ReturnsAllCustomers()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);
            var extractionParams = new ExtractionParameters(
                targetStructures: new[] { "Customers" },
                filterCriteria: new Dictionary<string, object>
                {
                    { "SchemaName", _testDbName },
                    { "ObjectName", "Customers" }
                }
            );

            // Act
            var result = await _connector.ExtractDataAsync(extractionParams);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.Equal(3, result.RecordCount); // We inserted 3 customers
            Assert.Equal(3, result.Data.Count);
            
            // Verify structure
            var firstCustomer = result.Data[0];
            Assert.True(firstCustomer.ContainsKey("CustomerId"));
            Assert.True(firstCustomer.ContainsKey("FirstName"));
            Assert.True(firstCustomer.ContainsKey("LastName"));
            Assert.True(firstCustomer.ContainsKey("Email"));
            Assert.True(firstCustomer.ContainsKey("IsActive"));

            // Clean up
            await _connector.DisconnectAsync();
        }

        [Fact]
        public async Task ExtractDataAsync_WithFilters_ReturnsFilteredOrders()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);
            var extractionParams = new ExtractionParameters(
                targetStructures: new[] { "Orders" },
                filterCriteria: new Dictionary<string, object>
                {
                    { "SchemaName", _testDbName },
                    { "ObjectName", "Orders" },
                    { "CustomerId", 1 }
                }
            );

            // Act
            var result = await _connector.ExtractDataAsync(extractionParams);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.RecordCount); // Customer ID 1 has 2 orders
            Assert.Equal(2, result.Data.Count);

            // Clean up
            await _connector.DisconnectAsync();
        }

        [Fact]
        public async Task ExtractDataAsync_WithPagination_ReturnsLimitedOrders()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);
            var extractionParams = new ExtractionParameters(
                targetStructures: new[] { "OrderItems" },
                filterCriteria: new Dictionary<string, object>
                {
                    { "SchemaName", _testDbName },
                    { "ObjectName", "OrderItems" }
                },
                maxRecords: 2,
                batchSize: 2 // This corresponds to the limit and offset in the original code
            );

            // Act
            var result = await _connector.ExtractDataAsync(extractionParams);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count); // Should return 2 items
            Assert.True(result.HasMoreRecords); // Total count should be greater than returned records

            // Clean up
            await _connector.DisconnectAsync();
        }

        #endregion

        #region Data Transformation Tests

        [Fact]
        public async Task TransformDataAsync_MapFields_TransformsData()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);
            
            // First extract some data
            var extractionParams = new ExtractionParameters(
                targetStructures: new[] { "Customers" },
                filterCriteria: new Dictionary<string, object>
                {
                    { "SchemaName", _testDbName },
                    { "ObjectName", "Customers" }
                }
            );
            
            var extractionResult = await _connector.ExtractDataAsync(extractionParams);
            
            // Setup transformation params with proper TransformationRule constructor
            var mappingRule = new TransformationRule(
                id: "map-rule",
                type: "Map",
                sourceFields: new[] { "FirstName", "LastName", "Email" },
                targetFields: new[] { "first_name", "last_name", "email_address" },
                parameters: new Dictionary<string, object>
                {
                    {
                        "mappings", new Dictionary<string, object>
                        {
                            { "FirstName", "first_name" },
                            { "LastName", "last_name" },
                            { "Email", "email_address" }
                        }
                    }
                }
            );
            
            var transformationParams = new TransformationParameters(
                rules: new List<TransformationRule> { mappingRule }
            );

            // Act
            var result = await _connector.TransformDataAsync(extractionResult.Data, transformationParams);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.TransformedData);
            var firstItem = result.TransformedData[0];
            Assert.True(firstItem.ContainsKey("first_name"));
            Assert.True(firstItem.ContainsKey("last_name"));
            Assert.True(firstItem.ContainsKey("email_address"));
            
            // Original keys should be gone
            Assert.False(firstItem.ContainsKey("FirstName"));
            Assert.False(firstItem.ContainsKey("LastName"));
            Assert.False(firstItem.ContainsKey("Email"));

            // Clean up
            await _connector.DisconnectAsync();
        }

        [Fact]
        public async Task TransformDataAsync_FilterActive_FiltersData()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);
            
            // First extract some data
            var extractionParams = new ExtractionParameters(
                targetStructures: new[] { "Customers" },
                filterCriteria: new Dictionary<string, object>
                {
                    { "SchemaName", _testDbName },
                    { "ObjectName", "Customers" }
                }
            );
            
            var extractionResult = await _connector.ExtractDataAsync(extractionParams);
            
            // Setup transformation params with proper TransformationRule constructor
            var filterRule = new TransformationRule(
                id: "filter-rule",
                type: "Filter",
                sourceFields: new[] { "IsActive" },
                targetFields: new string[0],
                condition: "IsActive = true",
                parameters: new Dictionary<string, object>
                {
                    { "condition", "IsActive = true" }
                }
            );
            
            var transformationParams = new TransformationParameters(
                rules: new List<TransformationRule> { filterRule }
            );

            // Act
            var result = await _connector.TransformDataAsync(extractionResult.Data, transformationParams);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.TransformedData);
            Assert.Equal(2, result.TransformedData.Count); // Only 2 customers are active
            Assert.All(result.TransformedData, item => Assert.True(Convert.ToBoolean(item["IsActive"])));

            // Clean up
            await _connector.DisconnectAsync();
        }

        #endregion
    }
} 