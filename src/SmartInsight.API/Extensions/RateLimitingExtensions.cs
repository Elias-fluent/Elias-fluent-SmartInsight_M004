using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.RateLimiting;

namespace SmartInsight.API.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds rate limiting services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global rate limiter for anonymous users
            options.AddFixedWindowLimiter("anonymous", limiterOptions =>
            {
                limiterOptions.PermitLimit = 20;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 5;
            });
            
            // Global rate limiter for authenticated users
            options.AddFixedWindowLimiter("authenticated", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 10;
            });
            
            // Configure global policy selector
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                // Use different limits for authenticated vs anonymous users
                var limiterName = httpContext.User.Identity?.IsAuthenticated == true
                    ? "authenticated"
                    : "anonymous";
                    
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limiterName == "authenticated" ? 100 : 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = limiterName == "authenticated" ? 10 : 5
                    });
            });
            
            // Configure response for rate limiting
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests. Please try again later."
                }, token);
            };
        });
        
        return services;
    }
} 