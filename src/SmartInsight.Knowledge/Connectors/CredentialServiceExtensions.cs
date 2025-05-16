using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Core.Interfaces;
using SmartInsight.Core.Entities;
using SmartInsight.Data.Repositories;

namespace SmartInsight.Knowledge.Connectors;

/// <summary>
/// Extension methods for registering credential services
/// </summary>
public static class CredentialServiceExtensions
{
    /// <summary>
    /// Adds credential management services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCredentialManagement(this IServiceCollection services)
    {
        // Register repository
        services.AddScoped<CredentialRepository>();
        services.AddEntityRepository<Credential, CredentialRepository>();
        
        // Register credential manager
        services.AddScoped<ICredentialManager, CredentialManager>();
        
        return services;
    }
} 