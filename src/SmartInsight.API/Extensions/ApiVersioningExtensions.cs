using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace SmartInsight.API.Extensions;

/// <summary>
/// Extension methods for configuring API versioning
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// Adds API versioning services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
    {
        // Add API versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-API-Version"),
                new QueryStringApiVersionReader("api-version"));
        });
        
        // Add API version explorer
        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
        
        return services;
    }
} 