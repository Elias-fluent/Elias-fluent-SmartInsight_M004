using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace SmartInsight.Tests.SQL.Integration.Containers
{
    /// <summary>
    /// A wrapper around the Testcontainers Qdrant container for integration tests
    /// </summary>
    public class QdrantContainer : IAsyncDisposable
    {
        private readonly IContainer _container;
        private bool _isDisposed;

        /// <summary>
        /// The URL to the Qdrant API
        /// </summary>
        public string ApiUrl => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(6333)}";

        /// <summary>
        /// Creates a new instance of the Qdrant container
        /// </summary>
        public QdrantContainer()
        {
            _container = new ContainerBuilder()
                .WithImage("qdrant/qdrant:latest")
                .WithPortBinding(Random.Shared.Next(6334, 6400), 6333)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(6333).ForPath("/healthz")))
                .Build();
        }

        /// <summary>
        /// Starts the Qdrant container asynchronously
        /// </summary>
        public async Task StartAsync()
        {
            await _container.StartAsync();
        }

        /// <summary>
        /// Disposes of the Qdrant container
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                await _container.DisposeAsync();
                _isDisposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
} 