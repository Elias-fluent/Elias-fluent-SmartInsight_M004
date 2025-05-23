using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartInsight.Core.Interfaces;
using SmartInsight.Knowledge.Connectors;
using SmartInsight.Tests.SQL.Common.Utilities;
using SmartInsight.Tests.SQL.Integration.Database;
using Xunit;
using Xunit.Abstractions;
using Testcontainers.MsSql;

namespace SmartInsight.Tests.SQL.Integration
{
    /// <summary>
    /// Integration tests for the MSSQLConnector class
    /// These tests require a SQL Server Docker container to be running
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("RequiresSetup", "MSSQL")]
    public class MSSQLConnectorIntegrationTests : TestBase, IAsyncLifetime
    {
        private readonly MSSQLConnector _connector;
        private readonly string _testDbName = "smartinsight_test_db";
        private MsSqlContainer _msSqlContainer;
        private Dictionary<string, string> _validConnectionParams;
        private readonly ServiceProvider _serviceProvider;
        private const int MSSQL_DEFAULT_PORT = 1433;

        public MSSQLConnectorIntegrationTests(ITestOutputHelper output) 
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
            
            _connector = new MSSQLConnector(
                _serviceProvider.GetRequiredService<ILogger<MSSQLConnector>>());

            // Setup SQL Server container
            _msSqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
                .WithPassword("StrongP@ssw0rd!")
                .Build();
        }

        public async Task InitializeAsync()
        {
            LogInfo("Starting SQL Server container for integration tests");
            await _msSqlContainer.StartAsync();
            
            // Setup connection parameters
            _validConnectionParams = new Dictionary<string, string>
            {
                { "server", _msSqlContainer.Hostname },
                { "port", _msSqlContainer.GetMappedPublicPort(MSSQL_DEFAULT_PORT).ToString() },
                { "database", "master" }, // Initially connect to master to create test database
                { "username", "sa" },
                { "password", "StrongP@ssw0rd!" },
                { "encrypt", "false" }
            };
            
            LogInfo($"SQL Server container started at {_validConnectionParams["server"]}:{_validConnectionParams["port"]}");
            
            try
            {
                // Create test database
                await CreateTestDatabaseAsync();
                
                // Update connection parameters to use the test database
                _validConnectionParams["database"] = _testDbName;
                
                // Create test schema and sample data
                await CreateTestSchemaAsync();
                
                LogInfo("Test schema setup completed successfully");
            }
            catch (Exception ex)
            {
                LogError("Failed to set up test database and schema", ex);
                throw;
            }
        }

        public async Task DisposeAsync()
        {
            if (_msSqlContainer != null)
            {
                LogInfo("Stopping SQL Server container");
                await _msSqlContainer.DisposeAsync();
            }
        }

        /// <summary>
        /// Creates the test database
        /// </summary>
        private async Task CreateTestDatabaseAsync()
        {
            LogInfo($"Creating test database '{_testDbName}'");
            
            var connectionString = $"Server={_validConnectionParams["server"]},{_validConnectionParams["port"]};Database=master;User Id={_validConnectionParams["username"]};Password={_validConnectionParams["password"]};TrustServerCertificate=True;";
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            string createDbCommand = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{_testDbName}') CREATE DATABASE [{_testDbName}]";
            await using (var cmd = new SqlCommand(createDbCommand, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }
            
            LogInfo($"Test database '{_testDbName}' created successfully");
        }

        /// <summary>
        /// Creates test schema and sample data for integration tests
        /// </summary>
        private async Task CreateTestSchemaAsync()
        {
            LogInfo($"Connecting to test database '{_testDbName}'");
            
            var connectionString = $"Server={_validConnectionParams["server"]},{_validConnectionParams["port"]};Database={_testDbName};User Id={_validConnectionParams["username"]};Password={_validConnectionParams["password"]};TrustServerCertificate=True;";
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Create Customers table
            LogInfo("Creating Customers table");
            string createCustomersTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
                CREATE TABLE Customers (
                    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
                    FirstName NVARCHAR(50) NOT NULL,
                    LastName NVARCHAR(50) NOT NULL,
                    Email NVARCHAR(100) NOT NULL UNIQUE,
                    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                    IsActive BIT NOT NULL DEFAULT 1
                )";
            await using (var cmd = new SqlCommand(createCustomersTable, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Create Orders table
            LogInfo("Creating Orders table");
            string createOrdersTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
                CREATE TABLE Orders (
                    OrderId INT IDENTITY(1,1) PRIMARY KEY,
                    CustomerId INT NOT NULL,
                    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
                    TotalAmount DECIMAL(10, 2) NOT NULL,
                    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
                    FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
                )";
            await using (var cmd = new SqlCommand(createOrdersTable, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Create OrderItems table
            LogInfo("Creating OrderItems table");
            string createOrderItemsTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
                CREATE TABLE OrderItems (
                    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
                    OrderId INT NOT NULL,
                    ProductName NVARCHAR(100) NOT NULL,
                    Quantity INT NOT NULL,
                    UnitPrice DECIMAL(10, 2) NOT NULL,
                    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
                )";
            await using (var cmd = new SqlCommand(createOrderItemsTable, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Check if tables already have data
            string checkDataCommand = "SELECT COUNT(*) FROM Customers";
            int customerCount = 0;
            await using (var cmd = new SqlCommand(checkDataCommand, connection))
            {
                customerCount = (int)await cmd.ExecuteScalarAsync();
            }

            if (customerCount == 0)
            {
                // Insert sample data into Customers
                LogInfo("Inserting sample data into Customers table");
                string insertCustomers = @"
                    INSERT INTO Customers (FirstName, LastName, Email, IsActive) VALUES
                    ('John', 'Doe', 'john.doe@example.com', 1),
                    ('Jane', 'Smith', 'jane.smith@example.com', 1),
                    ('Mike', 'Johnson', 'mike.johnson@example.com', 0)";
                await using (var cmd = new SqlCommand(insertCustomers, connection))
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
                await using (var cmd = new SqlCommand(insertOrders, connection))
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
                await using (var cmd = new SqlCommand(insertOrderItems, connection))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            else
            {
                LogInfo("Sample data already exists, skipping data insertion");
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
                { "schema", "dbo" }
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
                    { "SchemaName", "dbo" },
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
                    { "SchemaName", "dbo" },
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
                    { "SchemaName", "dbo" },
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
                    { "SchemaName", "dbo" },
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
                    { "SchemaName", "dbo" },
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
                condition: "IsActive = 1",
                parameters: new Dictionary<string, object>
                {
                    { "condition", "IsActive = 1" }
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
            
            // Check that IsActive is 1 (true) for all returned records
            foreach (var customer in result.TransformedData)
            {
                bool isActive = Convert.ToBoolean(customer["IsActive"]);
                Assert.True(isActive);
            }

            // Clean up
            await _connector.DisconnectAsync();
        }

        #endregion
    }
} 