using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.AI.SQL.Interfaces;
using SmartInsight.AI.SQL.Validators;

namespace SmartInsight.AI.SQL
{
    /// <summary>
    /// Extension methods for setting up SQL services in an IServiceCollection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds SQL parameter validation services to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddSqlParameterValidation(this IServiceCollection services)
        {
            // Register the base parameter validator
            services.AddSingleton<IParameterValidator, ParameterValidator>();
            
            // Register specialized validators
            services.AddSingleton<UserParameterValidator>();
            services.AddSingleton<DatabaseObjectValidator>();
            services.AddSingleton<SqlOperationValidator>();
            services.AddSingleton<ValueTypeValidator>();
            
            return services;
        }
        
        /// <summary>
        /// Adds SQL parameter extraction services to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddSqlParameterExtraction(this IServiceCollection services)
        {
            // Register the parameter extractor
            services.AddSingleton<IParameterExtractor, ParameterExtractor>();
            
            return services;
        }
        
        /// <summary>
        /// Adds all SQL template system services to the service collection
        /// </summary>
        /// <param name="services">The IServiceCollection to add the services to</param>
        /// <returns>The same service collection for method chaining</returns>
        public static IServiceCollection AddSqlTemplateSystem(this IServiceCollection services)
        {
            // Add parameter validation
            services.AddSqlParameterValidation();
            
            // Add parameter extraction
            services.AddSqlParameterExtraction();
            
            // Add other SQL template system services here as they are implemented
            // For example: template repository, template selector, SQL generator, etc.
            
            return services;
        }
    }
} 