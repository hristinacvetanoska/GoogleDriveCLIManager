namespace GoogleDriveCLIManager.Infrastructure
{
    using Microsoft.Extensions.DependencyInjection;
    using Spectre.Console.Cli;

    /// <summary>
    /// Adapts Microsoft.Extensions.DependencyInjection to work with Spectre.Console.Cli.
    /// Allows commands to receive their dependencies via constructor injection.
    /// </summary>
    public sealed class TypeRegistrar : ITypeRegistrar
    {
        private readonly IServiceCollection _services;

        public TypeRegistrar(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// Builds the service provider and returns a resolver for Spectre.Console.Cli.
        /// </summary>
        /// <returns>A <see cref="TypeResolver"/> wrapping the built service provider.</returns>
        public ITypeResolver Build()
            => new TypeResolver(_services.BuildServiceProvider());

        /// <summary>
        /// Registers a service type with its implementation type.
        /// </summary>
        /// <param name="service">The service interface type.</param>
        /// <param name="implementation">The concrete implementation type.</param>
        public void Register(Type service, Type implementation)
            => _services.AddSingleton(service, implementation);

        /// <summary>
        /// Registers a service type with an existing instance.
        /// </summary>
        /// <param name="service">The service interface type.</param>
        /// <param name="implementation">The existing instance to register.</param>
        public void RegisterInstance(Type service, object implementation)
            => _services.AddSingleton(service, implementation);

        /// <summary>
        /// Registers a service type with a factory function.
        /// </summary>
        /// <param name="service">The service interface type.</param>
        /// <param name="factory">The factory function that creates the instance.</param>
        public void RegisterLazy(Type service, Func<object> factory)
            => _services.AddSingleton(service, _ => factory());
    }

    /// <summary>
    /// Resolves types from the built Microsoft.Extensions.DependencyInjection service provider.
    /// Used by Spectre.Console.Cli to instantiate commands with their dependencies.
    /// </summary>
    public sealed class TypeResolver : ITypeResolver, IDisposable
    {
        private readonly IServiceProvider _provider;

        public TypeResolver(IServiceProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Resolves an instance of the specified type from the service provider.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <returns>The resolved instance, or null if the type is not registered.</returns>
        public object? Resolve(Type? type)
        {
            if (type is null) return null;
            return _provider.GetService(type);
        }

        /// <summary>
        /// Disposes the underlying service provider if it implements <see cref="IDisposable"/>.
        /// </summary>
        public void Dispose()
        {
            if (_provider is IDisposable disposable)
                disposable.Dispose();
        }
    }
}