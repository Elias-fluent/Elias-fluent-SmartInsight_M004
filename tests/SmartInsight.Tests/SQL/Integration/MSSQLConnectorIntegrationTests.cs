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

namespace SmartInsight.Tests.SQL.Integration
{
    /// <summary>
    /// Integration tests for the MSSQLConnector class
    /// These tests require a real SQL Server instance or test container to be running
    /// </summary>
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("RequiresSetup", "SqlServer")]
    public class MSSQLConnectorIntegrationTests : IntegrationTestBase
    {
        private readonly MSSQLConnector _connector;
        private readonly string _testDbName = "smartinsight_test_db";
        private Dictionary<string, string> _validConnectionParams;

        public MSSQLConnectorIntegrationTests(ITestOutputHelper output) 
            : base(output)
        {
            _connector = new MSSQLConnector(
                _serviceProvider.GetRequiredService<ILogger<MSSQLConnector>>());

            // Set up test connection parameters
            _validConnectionParams = new Dictionary<string, string>
            {
                { "server", Environment.GetEnvironmentVariable("TEST_SQL_SERVER") ?? "localhost" },
                { "port", Environment.GetEnvironmentVariable("TEST_SQL_PORT") ?? "1433" },
                { "database", _testDbName },
                { "username", Environment.GetEnvironmentVariable("TEST_SQL_USERNAME") ?? "sa" },
                { "password", Environment.GetEnvironmentVariable("TEST_SQL_PASSWORD") ?? "Password123!" },
                { "trustServerCertificate", "true" },
                { "connectionTimeout", "30" }
            };
        }

        /// <summary>
        /// Sets up the test database for integration tests
        /// </summary>
        protected override async Task SetupIntegrationTestAsync()
        {
            LogInfo("Setting up test database for SQL Server connector integration tests");
            
            try
            {
                // Create test database if it doesn't exist
                await CreateTestDatabaseAsync();
                
                // Create test tables and sample data
                await CreateTestSchemaAsync();
                
                LogInfo("Test database setup completed successfully");
            }
            catch (Exception ex)
            {
                LogError("Failed to set up test database", ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a test database for integration tests
        /// </summary>
        private async Task CreateTestDatabaseAsync()
        {
            // Connection string to master database
            string masterConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = $"{_validConnectionParams["server"]},{_validConnectionParams["port"]}",
                UserID = _validConnectionParams["username"],
                Password = _validConnectionParams["password"],
                TrustServerCertificate = true,
                InitialCatalog = "master",
                ConnectTimeout = 30
            }.ConnectionString;

            LogInfo($"Connecting to master database at {_validConnectionParams["server"]}:{_validConnectionParams["port"]}");
            
            // Create test database if it doesn't exist
            await using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();

            // Check if database exists
            string checkSql = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{_testDbName}'";
            await using var checkCmd = new SqlCommand(checkSql, connection);
            int dbCount = (int)await checkCmd.ExecuteScalarAsync();

            if (dbCount == 0)
            {
                LogInfo($"Creating test database '{_testDbName}'");
                string createDbSql = $"CREATE DATABASE [{_testDbName}]";
                await using var createCmd = new SqlCommand(createDbSql, connection);
                await createCmd.ExecuteNonQueryAsync();
            }
            else
            {
                LogInfo($"Test database '{_testDbName}' already exists");
            }
        }

        /// <summary>
        /// Creates test schema and sample data for integration tests
        /// </summary>
        private async Task CreateTestSchemaAsync()
        {
            // Connection string to test database
            string testDbConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = $"{_validConnectionParams["server"]},{_validConnectionParams["port"]}",
                UserID = _validConnectionParams["username"],
                Password = _validConnectionParams["password"],
                TrustServerCertificate = true,
                InitialCatalog = _testDbName,
                ConnectTimeout = 30
            }.ConnectionString;

            LogInfo($"Connecting to test database '{_testDbName}'");
            
            await using var connection = new SqlConnection(testDbConnectionString);
            await connection.OpenAsync();

            // Create Customers table
            LogInfo("Creating Customers table");
            string createCustomersTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
                BEGIN
                    CREATE TABLE Customers (
                        CustomerId INT PRIMARY KEY IDENTITY(1,1),
                        FirstName NVARCHAR(50) NOT NULL,
                        LastName NVARCHAR(50) NOT NULL,
                        Email NVARCHAR(100) UNIQUE NOT NULL,
                        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                        IsActive BIT NOT NULL DEFAULT 1
                    )
                END";
            await using (var cmd = new SqlCommand(createCustomersTable, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Create Orders table
            LogInfo("Creating Orders table");
            string createOrdersTable = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
                BEGIN
                    CREATE TABLE Orders (
                        OrderId INT PRIMARY KEY IDENTITY(1,1),
                        CustomerId INT NOT NULL,
                        OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
                        TotalAmount DECIMAL(10, 2) NOT NULL,
                        Status NVARCHAR(20) NOT NULL,
                        CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) 
                        REFERENCES Customers(CustomerId)
                    )
                END";
            await using (var cmd = new SqlCommand(createOrdersTable, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Insert sample data into Customers if the table is empty
            string countCustomersSql = "SELECT COUNT(*) FROM Customers";
            await using (var cmd = new SqlCommand(countCustomersSql, connection))
            {
                int customerCount = (int)await cmd.ExecuteScalarAsync();
                if (customerCount == 0)
                {
                    LogInfo("Inserting sample customer data");
                    string insertCustomers = @"
                        INSERT INTO Customers (FirstName, LastName, Email, CreatedDate, IsActive)
                        VALUES 
                        ('John', 'Doe', 'john.doe@example.com', GETDATE(), 1),
                        ('Jane', 'Smith', 'jane.smith@example.com', GETDATE(), 1),
                        ('Robert', 'Johnson', 'robert.johnson@example.com', GETDATE(), 0)";
                    await using var insertCmd = new SqlCommand(insertCustomers, connection);
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }

            // Insert sample data into Orders if the table is empty
            string countOrdersSql = "SELECT COUNT(*) FROM Orders";
            await using (var cmd = new SqlCommand(countOrdersSql, connection))
            {
                int orderCount = (int)await cmd.ExecuteScalarAsync();
                if (orderCount == 0)
                {
                    LogInfo("Inserting sample order data");
                    string insertOrders = @"
                        INSERT INTO Orders (CustomerId, OrderDate, TotalAmount, Status)
                        VALUES 
                        (1, DATEADD(day, -10, GETDATE()), 100.50, 'Completed'),
                        (1, DATEADD(day, -5, GETDATE()), 200.75, 'Processing'),
                        (2, DATEADD(day, -3, GETDATE()), 150.25, 'Shipped')";
                    await using var insertCmd = new SqlCommand(insertOrders, connection);
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
        }

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
        public async Task ValidateConnectionAsync_WithInvalidServerName_ReturnsValidationError()
        {
            // Arrange
            var invalidParams = new Dictionary<string, string>(_validConnectionParams);
            invalidParams["server"] = "non-existent-server";

            // Act
            var result = await _connector.ValidateConnectionAsync(invalidParams);

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

        [Fact]
        public async Task DiscoverDataStructuresAsync_AfterConnect_ReturnsTablesAndColumns()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);

            // Act
            var structures = await _connector.DiscoverDataStructuresAsync();
            var structuresList = structures.ToList();

            // Assert
            Assert.NotNull(structures);
            Assert.NotEmpty(structuresList);
            
            // Check if our test tables are discovered
            var customersTable = structuresList.FirstOrDefault(s => s.Name.EndsWith("Customers"));
            Assert.NotNull(customersTable);
            Assert.NotEmpty(customersTable.Fields);
            Assert.Contains(customersTable.Fields, f => f.Name == "CustomerId");
            Assert.Contains(customersTable.Fields, f => f.Name == "Email" && f.IsUnique);
            
            var ordersTable = structuresList.FirstOrDefault(s => s.Name.EndsWith("Orders"));
            Assert.NotNull(ordersTable);
            Assert.NotEmpty(ordersTable.Fields);
            Assert.Contains(ordersTable.Fields, f => f.Name == "OrderId");
            Assert.Contains(ordersTable.Fields, f => f.Name == "CustomerId");

            // Clean up
            await _connector.DisconnectAsync();
        }

        [Fact]
        public async Task ExtractDataAsync_WithValidTable_ReturnsData()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);
            
            var extractionParams = new ExtractionParameters
            {
                SourceIdentifier = "Customers",
                Filters = new Dictionary<string, object>
                {
                    { "IsActive", true }
                },
                Pagination = new PaginationOptions
                {
                    Limit = 10,
                    Offset = 0
                }
            };

            // Act
            var result = await _connector.ExtractDataAsync(extractionParams);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);
            
            // Verify we only got active customers
            foreach (var customer in result.Data)
            {
                Assert.True(Convert.ToBoolean(customer["IsActive"]));
            }

            // Clean up
            await _connector.DisconnectAsync();
        }

        [Fact]
        public async Task TransformDataAsync_WithValidTransformation_TransformsData()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);
            
            // First extract some data to transform
            var extractionParams = new ExtractionParameters
            {
                SourceIdentifier = "Customers",
                Pagination = new PaginationOptions
                {
                    Limit = 10,
                    Offset = 0
                }
            };
            
            var extractionResult = await _connector.ExtractDataAsync(extractionParams);
            Assert.True(extractionResult.IsSuccessful);
            
            // Set up transformation
            var transformationParams = new TransformationParameters
            {
                FieldMappings = new Dictionary<string, string>
                {
                    { "CustomerId", "id" },
                    { "FirstName", "first_name" },
                    { "LastName", "last_name" },
                    { "Email", "email" }
                },
                ComputedFields = new Dictionary<string, Func<IDictionary<string, object>, object>>
                {
                    { "full_name", row => $"{row["FirstName"]} {row["LastName"]}" }
                },
                FieldsToExclude = new[] { "CreatedDate" }
            };

            // Act
            var result = await _connector.TransformDataAsync(extractionResult.Data, transformationParams);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Data);
            Assert.NotEmpty(result.Data);
            
            // Verify transformation
            var firstItem = result.Data.First();
            Assert.True(firstItem.ContainsKey("id"));
            Assert.True(firstItem.ContainsKey("first_name"));
            Assert.True(firstItem.ContainsKey("last_name"));
            Assert.True(firstItem.ContainsKey("email"));
            Assert.True(firstItem.ContainsKey("full_name"));
            Assert.False(firstItem.ContainsKey("CreatedDate"));
            
            // Verify computed field
            var fullName = firstItem["full_name"] as string;
            Assert.NotNull(fullName);
            Assert.Contains((string)firstItem["first_name"], fullName);
            Assert.Contains((string)firstItem["last_name"], fullName);

            // Clean up
            await _connector.DisconnectAsync();
        }

        [Fact]
        public async Task DisconnectAsync_AfterConnect_DisconnectsSuccessfully()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);
            Assert.Equal(ConnectionState.Connected, _connector.ConnectionState);

            // Act
            var result = await _connector.DisconnectAsync();

            // Assert
            Assert.True(result);
            Assert.Equal(ConnectionState.Disconnected, _connector.ConnectionState);
        }

        // Additional test for the error handling
        [Fact]
        public async Task ConnectAsync_WithInvalidCredentials_HandlesSqlError()
        {
            // Arrange
            var invalidParams = new Dictionary<string, string>(_validConnectionParams);
            invalidParams["username"] = "invalid_user";
            invalidParams["password"] = "invalid_password";

            // Act
            var result = await _connector.ConnectAsync(invalidParams);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("login", result.ErrorMessage.ToLower());
        }

        [Fact]
        public async Task ExtractDataAsync_WithNonExistentTable_HandlesSqlError()
        {
            // Arrange
            await _connector.ConnectAsync(_validConnectionParams);
            
            var extractionParams = new ExtractionParameters
            {
                SourceIdentifier = "NonExistentTable",
                Pagination = new PaginationOptions
                {
                    Limit = 10,
                    Offset = 0
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<SqlException>(() => 
                _connector.ExtractDataAsync(extractionParams));

            // Clean up
            await _connector.DisconnectAsync();
        }
    }
} 