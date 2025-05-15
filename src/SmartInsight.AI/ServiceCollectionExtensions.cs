using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartInsight.AI.Clients;
using SmartInsight.AI.Intent;
using SmartInsight.AI.Interfaces;
using SmartInsight.AI.Models;
using SmartInsight.AI.Options;

namespace SmartInsight.AI
{
    /// <summary>
    /// Extension methods for IServiceCollection to register AI services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Ollama client services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">Configuration containing Ollama settings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOllamaClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<OllamaOptions>(configuration.GetSection("Ollama"));
            services.AddHttpClient<IOllamaClient, OllamaClient>();
            
            return services;
        }

        /// <summary>
        /// Adds intent detection services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">Configuration containing intent detection settings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddIntentDetection(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register options
            services.Configure<IntentDetectionOptions>(configuration.GetSection("IntentDetection"));
            
            // Register intent detector
            services.AddScoped<IIntentDetector, IntentDetector>();
            
            // Register reasoning engine
            services.AddScoped<IReasoningEngine, ReasoningEngine>();
            
            return services;
        }

        /// <summary>
        /// Adds all SmartInsight AI services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">Configuration containing AI settings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSmartInsightAI(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOllamaClient(configuration);
            services.AddIntentDetection(configuration);
            
            return services;
        }
    }
} 