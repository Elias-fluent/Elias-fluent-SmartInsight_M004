using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.History.Interfaces;

namespace SmartInsight.History;

/// <summary>
/// Extension methods for configuring History services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds conversation memory services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConversationMemory(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register the conversation memory service
        services.AddScoped<IConversationMemory, ConversationMemory>();
        
        return services;
    }
} 