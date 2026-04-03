using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace FishApp.Api.Tests
{
      public abstract class TestFixture<TProgram> : IDisposable where TProgram : class
    {
        private readonly List<Action<WebHostBuilderContext, IConfigurationBuilder>> configureAppConfiguration = new List<Action<WebHostBuilderContext, IConfigurationBuilder>>();
        private readonly List<Action<IServiceCollection>> configureServices = new List<Action<IServiceCollection>>();
        private readonly List<Action<IServiceCollection>> configureTestServices = new List<Action<IServiceCollection>>();

        private WebApplicationFactory<TProgram>? factory;

        protected TestFixture()
        {
            this.LoggerFactory = new LoggerFactory();
            this.InitializeInternal();
        }

        private void InitializeInternal()
        {
            this.Initialize();
        }

        protected virtual void Initialize()
        {
        }

        public TestFixture<TProgram> ReplaceService<TInterface>(TInterface instance) where TInterface : class
        {
            if (this.factory != null)
            {
                // If the server is already created, we can't replace services.
                // Reset factory to force recreation on next use.
                return this;
            }

            this.configureServices.Add(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TInterface));
                if (descriptor != null)
                {
                    services.Replace(new ServiceDescriptor(typeof(TInterface), provider => instance, descriptor.Lifetime));
                }
            });

            return this;
        }

        public TestFixture<TProgram> ConfigureService(Action<IServiceCollection> action)
        {
            if (this.factory != null)
            {
                // If the server is already created, we can't replace services.
                // Reset factory to force recreation on next use.
                return this;
            }

            this.configureServices.Add(s => action(s));

            return this;
        }

        public TestFixture<TProgram> ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> action)
        {
            if (this.factory != null)
            {
                return this;
            }

            this.configureAppConfiguration.Add(action);

            return this;
        }

        public TestFixture<TProgram> ConfigureTestService(Action<IServiceCollection> action)
        {
            if (this.factory != null)
            {
                // If the server is already created, we can't replace services.
                // Reset factory to force recreation on next use.
                return this;
            }

            this.configureTestServices.Add(s => action(s));

            return this;
        }

        public TestFixture<TProgram> PostConfigure<TOptions>(Action<TOptions> options) where TOptions : class
        {
            return this.ConfigureTestService(s =>
            {
                s.PostConfigure(options);
            });
        }

        protected WebApplicationFactory<TProgram> GetOrCreateFactory()
        {
            if (this.factory != null)
            {
                return this.factory;
            }

            this.factory = new WebApplicationFactory<TProgram>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((context, configurationBuilder) =>
                    {
                        foreach (var configure in this.configureAppConfiguration)
                        {
                            configure(context, configurationBuilder);
                        }
                    });

                    builder.ConfigureServices(services =>
                    {
                        services.AddSingleton<ILoggerFactory>(this.LoggerFactory);

                        foreach (var configure in this.configureServices)
                        {
                            configure(services);
                        }
                    });

                    builder.ConfigureTestServices(services =>
                    {
                        foreach (var configure in this.configureTestServices)
                        {
                            configure(services);
                        }
                    });

                    builder.UseTestServer();
                });

            return this.factory;
        }

        public async Task RunScopedAsync(Func<IServiceProvider, Task> action)
        {
            var factory = this.GetOrCreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            await action(scope.ServiceProvider);
        }

        public async Task<TResult> RunScopedAsync<TResult>(Func<IServiceProvider, Task<TResult>> action)
        {
            var factory = this.GetOrCreateFactory();
            await using var scope = factory.Services.CreateAsyncScope();
            return await action(scope.ServiceProvider);
        }

        public LoggerFactory LoggerFactory { get; }

        public void ResetFactory()
        {
            this.configureAppConfiguration.Clear();
            this.configureServices.Clear();
            this.configureTestServices.Clear();

            this.factory?.Dispose();
            this.factory = null;

            this.InitializeInternal();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.ResetFactory();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
