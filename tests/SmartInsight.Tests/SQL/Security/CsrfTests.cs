using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Security
{
    /// <summary>
    /// Tests to verify Cross-Site Request Forgery (CSRF) protections
    /// </summary>
    public class CsrfTests : SecurityTestBase
    {
        public CsrfTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task State_ChangingRequests_RequireAntiCsrfToken()
        {
            // Arrange
            var token = await GetAuthenticationToken();
            
            // State-changing endpoints to test
            var endpoints = new[]
            {
                "/api/v1/users",                  // POST to create user
                "/api/v1/users/12345",            // PUT to update user
                "/api/v1/users/12345",            // DELETE to remove user
                "/api/v1/datasources",            // POST to create datasource
                "/api/v1/tenants/configuration"   // PUT to update tenant config
            };

            foreach (var endpoint in endpoints)
            {
                // Act
                // First - get CSRF token from the server via a GET request
                var getResponse = await GetWithAuthAsync(endpoint, token);
                string csrfToken = ExtractCsrfToken(await getResponse.Content.ReadAsStringAsync());
                
                // Try POST/PUT/DELETE without CSRF token
                var requestWithoutCsrf = new HttpRequestMessage(HttpMethod.Post, endpoint);
                requestWithoutCsrf.Headers.Add("Authorization", $"Bearer {token}");
                requestWithoutCsrf.Content = new StringContent("{\"test\":\"data\"}", 
                    System.Text.Encoding.UTF8, "application/json");
                
                var responseWithoutCsrf = await _client.SendAsync(requestWithoutCsrf);
                
                // Now try with CSRF token
                var requestWithCsrf = new HttpRequestMessage(HttpMethod.Post, endpoint);
                requestWithCsrf.Headers.Add("Authorization", $"Bearer {token}");
                requestWithCsrf.Headers.Add("X-CSRF-TOKEN", csrfToken); // Add CSRF token header
                requestWithCsrf.Content = new StringContent("{\"test\":\"data\"}", 
                    System.Text.Encoding.UTF8, "application/json");
                
                var responseWithCsrf = await _client.SendAsync(requestWithCsrf);
                
                // Assert
                // Request without CSRF token should be rejected
                Assert.Equal(HttpStatusCode.Forbidden, responseWithoutCsrf.StatusCode);
                
                // Request with valid CSRF token should be accepted (might still fail for other reasons)
                // So we're just checking it's not a 403 Forbidden due to CSRF protection
                Assert.NotEqual(HttpStatusCode.Forbidden, responseWithCsrf.StatusCode);
                
                _output.WriteLine($"CSRF protection verified for {endpoint}");
            }
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task ReadOnlyRequests_DoNotRequireAntiCsrfToken()
        {
            // Arrange
            var token = await GetAuthenticationToken();
            
            // Read-only endpoints to test
            var endpoints = new[]
            {
                "/api/v1/users",               // GET to list users
                "/api/v1/users/12345",         // GET to view user
                "/api/v1/datasources",         // GET to list datasources
                "/api/v1/tenants"              // GET to list tenants
            };

            foreach (var endpoint in endpoints)
            {
                // Act
                // Try GET request without CSRF token
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Add("Authorization", $"Bearer {token}");
                
                var response = await _client.SendAsync(request);
                
                // Assert
                // Read-only request should not be blocked by CSRF protection
                Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
                
                _output.WriteLine($"Read-only endpoint correctly accessible without CSRF token: {endpoint}");
            }
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task CsrfTokens_AreScopedPerSession()
        {
            // Arrange
            // Get authentication token for 2 different sessions
            var token1 = await GetAuthenticationToken();
            var token2 = await GetAuthenticationToken("user2", "password2");
            
            // Get CSRF token from 1st session
            var getResponse1 = await GetWithAuthAsync("/api/v1/users", token1);
            string csrfToken1 = ExtractCsrfToken(await getResponse1.Content.ReadAsStringAsync());
            
            // Act
            // Try to use CSRF token from session 1 with session 2's auth token
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users");
            request.Headers.Add("Authorization", $"Bearer {token2}");
            request.Headers.Add("X-CSRF-TOKEN", csrfToken1); // CSRF token from session 1
            request.Content = new StringContent("{\"test\":\"data\"}", 
                System.Text.Encoding.UTF8, "application/json");
            
            var response = await _client.SendAsync(request);
            
            // Assert
            // Request should be rejected due to token from different session
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            
            _output.WriteLine("CSRF tokens correctly scoped per session");
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task CsrfProtection_ViaDoubleSubmitCookie()
        {
            // Arrange
            var token = await GetAuthenticationToken();
            
            // Act
            // First - get CSRF token from the server via a GET request
            var getResponse = await GetWithAuthAsync("/api/v1/csrf-token", token);
            
            // Check if CSRF cookie was set
            bool hasCsrfCookie = getResponse.Headers.Contains("Set-Cookie") && 
                                getResponse.Headers.GetValues("Set-Cookie")
                                    .Any(c => c.Contains("XSRF-TOKEN"));
            
            // Try request with mismatched token (header doesn't match cookie)
            var requestWithMismatch = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users");
            requestWithMismatch.Headers.Add("Authorization", $"Bearer {token}");
            requestWithMismatch.Headers.Add("X-CSRF-TOKEN", "invalid-token");
            requestWithMismatch.Content = new StringContent("{\"test\":\"data\"}", 
                System.Text.Encoding.UTF8, "application/json");
            
            var responseMismatch = await _client.SendAsync(requestWithMismatch);
            
            // Assert
            Assert.True(hasCsrfCookie, "CSRF cookie should be set for double-submit verification");
            Assert.Equal(HttpStatusCode.Forbidden, responseMismatch.StatusCode);
            
            _output.WriteLine("Double-submit cookie CSRF protection working correctly");
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task CsrfTokens_AreRotatedAfterAuthentication()
        {
            // Arrange
            // Make unauthenticated request to get initial token
            var initialResponse = await _client.GetAsync("/api/v1/csrf-token");
            string initialToken = ExtractCsrfToken(await initialResponse.Content.ReadAsStringAsync());
            
            // Act
            // Log in to get authenticated session
            var loginData = new
            {
                Username = "testuser",
                Password = "password123",
                CsrfToken = initialToken
            };
            
            var loginResponse = await PostJsonAsync("/api/v1/auth/login", loginData);
            string postLoginToken = ExtractCsrfToken(await loginResponse.Content.ReadAsStringAsync());
            
            // Assert
            Assert.NotEqual(initialToken, postLoginToken);
            _output.WriteLine("CSRF token correctly rotated after authentication");
        }

        #region Helper Methods

        private async Task<string> GetAuthenticationToken()
        {
            return await GetAuthenticationToken("testuser", "password123");
        }

        private async Task<string> GetAuthenticationToken(string username, string password)
        {
            var loginData = new
            {
                Username = username,
                Password = password
            };
            
            var response = await PostJsonAsync("/api/v1/auth/login", loginData);
            return await ExtractTokenFromResponse(response);
        }

        private string ExtractCsrfToken(string html)
        {
            // In a real test, you would parse this from the response
            // This is just a placeholder implementation
            
            // For API responses, might be in an HTTP header or in a JSON response
            if (html.Contains("\"csrfToken\":"))
            {
                var startIndex = html.IndexOf("\"csrfToken\":\"") + "\"csrfToken\":\"".Length;
                var endIndex = html.IndexOf("\"", startIndex);
                return html.Substring(startIndex, endIndex - startIndex);
            }
            
            // For HTML responses, might be in a meta tag or form input
            if (html.Contains("name=\"_csrf\""))
            {
                var startIndex = html.IndexOf("value=\"", html.IndexOf("name=\"_csrf\"")) + "value=\"".Length;
                var endIndex = html.IndexOf("\"", startIndex);
                return html.Substring(startIndex, endIndex - startIndex);
            }
            
            // Default token for testing
            return "test-csrf-token";
        }

        #endregion
    }
} 