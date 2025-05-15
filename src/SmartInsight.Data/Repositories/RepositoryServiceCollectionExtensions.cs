using Microsoft.Extensions.DependencyInjection;
using SmartInsight.Core.Interfaces;

namespace SmartInsight.Data.Repositories;

/// <summary>
/// Extension methods for setting up repository services in an IServiceCollection
/// </summary>
public static class RepositoryServiceCollectionExtensions
{
    /// <summary>
    /// Adds repositories and unit of work to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register the UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register the generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }
    
    /// <summary>
    /// Adds entity-specific repositories to the service collection
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TRepository">Repository type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEntityRepository<TEntity, TRepository>(
        this IServiceCollection services)
        where TEntity : class
        where TRepository : class, IRepository<TEntity>
    {
        services.AddScoped<IRepository<TEntity>, TRepository>();
        return services;
    }
} 