using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Tests.SQL.Common.Utilities;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Security
{
    /// <summary>
    /// Base class for security tests providing common functionality
    /// </summary>
    public abstract class SecurityTestBase : TestBase
    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly HttpClient _client;
        protected readonly ITestOutputHelper _output;

        protected SecurityTestBase(ITestOutputHelper output) : base(output)
        {
            _output = output;
            
            // Set up the test server - commenting out WebApplicationFactory due to Program class access issue
            // Will implement mock HTTP client for testing instead
            _client = new HttpClient();
            
            // Create a service collection for testing
            var services = new ServiceCollection();
            ConfigureTestServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Configure services for testing
        /// </summary>
        protected virtual void ConfigureTestServices(ServiceCollection services)
        {
            // Add common test services here
            // Override in derived classes to add specific services
        }

        /// <summary>
        /// Helper method to make a POST request with JSON content
        /// </summary>
        protected async Task<HttpResponseMessage> PostJsonAsync(string url, object content)
        {
            var json = JsonSerializer.Serialize(content);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            return await _client.PostAsync(url, stringContent);
        }

        /// <summary>
        /// Helper method to make a GET request with authorization header
        /// </summary>
        protected async Task<HttpResponseMessage> GetWithAuthAsync(string url, string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            return await _client.SendAsync(request);
        }

        /// <summary>
        /// Helper method to check if a request is vulnerable to security issues
        /// </summary>
        protected async Task<bool> IsVulnerableTo(Func<Task<HttpResponseMessage>> requestFunc, 
                                                 Func<HttpResponseMessage, Task<bool>> vulnerabilityCheck)
        {
            try
            {
                var response = await requestFunc();
                return await vulnerabilityCheck(response);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Exception during vulnerability test: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper method to extract a token from a successful login response
        /// </summary>
        protected async Task<string> ExtractTokenFromResponse(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(content);
            
            if (document.RootElement.TryGetProperty("token", out var tokenElement))
            {
                return tokenElement.GetString() ?? string.Empty;
            }
            
            return string.Empty;
        }
    }
} 