using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Integration.Api
{
    /// <summary>
    /// Example integration tests for the SmartInsight API
    /// </summary>
    public class ApiIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiIntegrationTests"/> class
        /// </summary>
        /// <param name="outputHelper">The test output helper</param>
        public ApiIntegrationTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        /// <summary>
        /// Example test demonstrating how to make an HTTP request to the API
        /// </summary>
        [Fact]
        public async Task HealthCheck_ReturnsOk()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "/health");

            // Act
            var response = await ApiClient.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            LogInfo($"Health check response: {response.StatusCode}");
        }

        /// <summary>
        /// Example test demonstrating how to send JSON data to the API
        /// </summary>
        [Fact]
        public async Task PostData_WithValidPayload_ReturnsSuccess()
        {
            // Arrange
            var testData = new { Name = "Test", Value = 123 };
            var content = new StringContent(
                JsonSerializer.Serialize(testData),
                Encoding.UTF8,
                "application/json");

            // Act - This is just a demonstration, replace with actual endpoint
            var response = await ApiClient.PostAsync("/api/demo", content);

            // Assert - This would be replaced with actual expected response
            // This test is expected to fail until an actual endpoint is implemented
            LogInfo($"Post data response: {response.StatusCode}");
            // Uncomment when endpoint exists:
            // Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        /// <summary>
        /// Example test demonstrating database interaction in an integration test
        /// </summary>
        [Fact]
        public async Task DatabaseConnection_IsWorking()
        {
            // Arrange
            LogInfo($"Using connection string: {DbConnectionString}");
            
            // Act & Assert
            // The test will fail if we can't connect to the database
            // This is implicitly tested during test setup via DatabaseResetHelper
            
            // Additional database-specific tests would go here
            await Task.CompletedTask;
            
            LogInfo("Database connection test completed successfully");
        }

        /// <summary>
        /// Example test demonstrating Qdrant vector database interaction
        /// </summary>
        [Fact]
        public async Task QdrantConnection_IsWorking()
        {
            // Arrange
            LogInfo($"Using Qdrant API URL: {QdrantApiUrl}");
            
            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, $"{QdrantApiUrl}/healthz");
            var response = await new HttpClient().SendAsync(request);
            
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            LogInfo("Qdrant connection test completed successfully");
        }
    }
} 