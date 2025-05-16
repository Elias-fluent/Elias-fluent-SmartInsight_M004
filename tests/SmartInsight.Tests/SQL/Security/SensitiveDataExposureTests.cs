using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Security
{
    /// <summary>
    /// Tests to identify potential sensitive data exposure vulnerabilities
    /// </summary>
    public class SensitiveDataExposureTests : SecurityTestBase
    {
        private readonly List<string> _sensitiveDataPatterns = new List<string>
        {
            // Passwords
            "password",
            "passwd",
            "pwd",
            
            // Encryption keys
            "key",
            "secret",
            "private",
            
            // Connection strings
            "connectionString",
            "connStr",
            
            // Personal data
            "ssn",
            "socialSecurity",
            "dob",
            "dateOfBirth",
            "creditCard",
            "ccNumber",
            
            // API keys/tokens
            "apiKey",
            "token",
            "accessKey",
            "authToken"
        };

        public SensitiveDataExposureTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task ErrorResponses_DoNotLeakSensitiveInformation()
        {
            // Arrange
            var endpoints = new[]
            {
                "/api/v1/users/nonexistent",           // Non-existent resource
                "/api/v1/datasources/invalid",         // Invalid ID format
                "/api/v1/auth/login"                   // Login with missing credentials
            };
            
            foreach (var endpoint in endpoints)
            {
                // Act
                // Make request that will trigger an error
                var response = await _client.GetAsync(endpoint);
                var content = await response.Content.ReadAsStringAsync();
                
                // Assert
                // Response should not contain full stack traces or sensitive system information
                Assert.DoesNotContain("Exception", content);
                Assert.DoesNotContain("at System.", content);
                Assert.DoesNotContain("StackTrace", content);
                Assert.DoesNotContain(":\\", content); // File paths
                
                // Check for various sensitive data patterns
                bool containsSensitiveData = false;
                foreach (var pattern in _sensitiveDataPatterns)
                {
                    if (ContainsSensitiveData(content, pattern))
                    {
                        containsSensitiveData = true;
                        _output.WriteLine($"Sensitive data found in error response: {pattern}");
                        break;
                    }
                }
                
                Assert.False(containsSensitiveData, $"Error response for {endpoint} contains sensitive data");
                _output.WriteLine($"Error response for {endpoint} correctly sanitized");
            }
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task ApiResponses_DoNotIncludeSensitiveFields()
        {
            // Arrange
            var token = await GetAuthenticationToken();
            
            var userEndpoints = new[]
            {
                "/api/v1/users",                // List users
                "/api/v1/users/current",        // Current user profile
                "/api/v1/users/12345"           // Specific user by ID
            };
            
            foreach (var endpoint in userEndpoints)
            {
                // Act
                var response = await GetWithAuthAsync(endpoint, token);
                
                // Only process successful responses
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Assert
                    // Response should not contain password hashes or other sensitive data
                    Assert.DoesNotContain("\"password\"", content);
                    Assert.DoesNotContain("\"passwordHash\"", content);
                    Assert.DoesNotContain("\"salt\"", content);
                    Assert.DoesNotContain("\"securityStamp\"", content);
                    
                    _output.WriteLine($"API response for {endpoint} correctly excludes sensitive fields");
                }
            }
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task ConnectionStrings_AreEncryptedOrMasked()
        {
            // Arrange
            var token = await GetAuthenticationToken();
            
            // Endpoints that might return connection string data
            var endpoints = new[]
            {
                "/api/v1/datasources",
                "/api/v1/datasources/12345",
                "/api/v1/admin/system-config"
            };
            
            foreach (var endpoint in endpoints)
            {
                // Act
                var response = await GetWithAuthAsync(endpoint, token);
                
                // Only process successful responses
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Look for common connection string patterns
                    bool hasExposedConnectionString = 
                        Regex.IsMatch(content, @"Server\s*=.*;") ||          // SQL Server
                        Regex.IsMatch(content, @"Host\s*=.*;") ||            // PostgreSQL
                        Regex.IsMatch(content, @"mongodb://[^'""]+") ||      // MongoDB
                        Regex.IsMatch(content, @"Data Source\s*=.*;");       // Generic ADO.NET
                    
                    // Assert
                    Assert.False(hasExposedConnectionString, 
                        $"Endpoint {endpoint} may expose unencrypted connection strings");
                    
                    _output.WriteLine($"Connection strings properly protected in {endpoint}");
                }
            }
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task PasswordStorage_UsesSecureHashing()
        {
            // This test checks if we can detect weak password storage practices
            // by examining how the system handles passwords during user creation
            
            // Arrange
            var token = await GetAuthenticationToken();
            
            var newUser = new
            {
                Username = "securitytest",
                Email = "security@test.com",
                Password = "SecurePassword123!"
            };
            
            // Act
            // Create a new user
            var createResponse = await PostJsonAsync("/api/v1/users", newUser);
            
            // If creation succeeded, try to retrieve the user data
            if (createResponse.IsSuccessStatusCode)
            {
                var getUserResponse = await _client.GetAsync("/api/v1/users");
                var content = await getUserResponse.Content.ReadAsStringAsync();
                
                // Assert
                // The raw password should never appear in responses
                Assert.DoesNotContain("SecurePassword123!", content);
                
                // Password hashes should not use weak algorithms
                bool hasWeakHash = 
                    content.Contains("\"md5\"") ||
                    content.Contains("\"sha1\"");
                
                Assert.False(hasWeakHash, "System may be using weak password hashing algorithms");
                
                _output.WriteLine("Password storage appears to use secure hashing");
            }
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task SensitiveOperations_RequireHttps()
        {
            // This test simulates a non-HTTPS request to verify that sensitive operations
            // are rejected if not using secure transport
            
            // Arrange
            // Create a new client that doesn't follow redirects
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
            
            var sensitiveEndpoints = new[]
            {
                "/api/v1/auth/login",
                "/api/v1/users", // POST
                "/api/v1/datasources" // POST
            };
            
            foreach (var endpoint in sensitiveEndpoints)
            {
                // Act
                // Create a request that explicitly uses HTTP instead of HTTPS
                var request = new HttpRequestMessage(HttpMethod.Post, 
                    endpoint.Replace("https://", "http://"));
                
                request.Content = new StringContent("{\"test\":\"data\"}", 
                    System.Text.Encoding.UTF8, "application/json");
                
                var response = await client.SendAsync(request);
                
                // Assert
                // Should either redirect to HTTPS or reject the request
                Assert.True(
                    response.StatusCode == HttpStatusCode.MovedPermanently ||
                    response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.BadRequest ||
                    response.StatusCode == HttpStatusCode.Forbidden,
                    $"Sensitive endpoint {endpoint} may accept non-HTTPS connections");
                
                _output.WriteLine($"Endpoint {endpoint} correctly requires HTTPS");
            }
        }

        [Fact(Skip = "Integration test requiring API setup")]
        public async Task TransportSecurity_Headers_ArePresent()
        {
            // Arrange
            var endpoints = new[]
            {
                "/",
                "/api/v1/users",
                "/api/v1/auth/login"
            };
            
            foreach (var endpoint in endpoints)
            {
                // Act
                var response = await _client.GetAsync(endpoint);
                
                // Assert
                // Check for important security headers
                bool hasHstsHeader = response.Headers.Contains("Strict-Transport-Security");
                
                Assert.True(hasHstsHeader, 
                    $"HSTS header missing on {endpoint} - may allow insecure connections");
                
                if (hasHstsHeader)
                {
                    var hstsValue = response.Headers.GetValues("Strict-Transport-Security").FirstOrDefault();
                    Assert.Contains("max-age=", hstsValue);
                    
                    _output.WriteLine($"HSTS header correctly configured on {endpoint}");
                }
            }
        }

        #region Helper Methods

        private bool ContainsSensitiveData(string content, string sensitivePattern)
        {
            // Check for JSON properties with this pattern
            var regex = new Regex($"\"{sensitivePattern}\"\\s*:\\s*\"[^\"]+\"", 
                RegexOptions.IgnoreCase);
            
            return regex.IsMatch(content);
        }

        private async Task<string> GetAuthenticationToken()
        {
            var loginData = new
            {
                Username = "adminuser",
                Password = "adminpassword"
            };
            
            var response = await PostJsonAsync("/api/v1/auth/login", loginData);
            return await ExtractTokenFromResponse(response);
        }

        #endregion
    }
} 