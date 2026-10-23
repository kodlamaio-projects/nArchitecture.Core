using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NArchitecture.Core.Mediator.DependencyInjection;

public static class MediatorServiceCollectionExtensions
{
    private static readonly Type[] _handlerInterfaces = [typeof(IRequestHandler<,>), typeof(IStreamRequestHandler<,>)];
    private static readonly Type[] _behaviorInterfaces = [typeof(IPipelineBehavior<,>), typeof(IStreamPipelineBehavior<,>)];

    /// <summary>Registers <see cref="IMediator"/>/<see cref="ISender"/>, scans the configured assemblies for handlers and adds behaviors.</summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, Action<MediatorConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        MediatorConfiguration configuration = new();
        configure(configuration);
        return services.AddMediator(configuration);
    }

    /// <summary>Registers <see cref="IMediator"/>/<see cref="ISender"/> and scans the given assemblies for handlers.</summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies) =>
        services.AddMediator(cfg => cfg.RegisterServicesFromAssemblies(assemblies));

    public static IServiceCollection AddMediator(this IServiceCollection services, MediatorConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAdd(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), configuration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ISender), sp => sp.GetRequiredService<IMediator>(), configuration.Lifetime));

        foreach (Assembly assembly in configuration.AssembliesToScan)
            registerHandlersFromAssembly(services, assembly, configuration.Lifetime);

        foreach (ServiceDescriptor descriptor in configuration.BehaviorDescriptors)
            services.Add(descriptor);

        return services;
    }

    private static void registerHandlersFromAssembly(IServiceCollection services, Assembly assembly, ServiceLifetime lifetime)
    {
        IEnumerable<Type> concreteTypes = assembly
            .DefinedTypes.Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Select(t => t.AsType());

        foreach (Type implementationType in concreteTypes)
        {
            foreach (Type serviceType in implementationType.GetInterfaces().Where(i => i.IsGenericType))
            {
                Type definition = serviceType.GetGenericTypeDefinition();

                if (_handlerInterfaces.Contains(definition))
                    services.TryAdd(new ServiceDescriptor(serviceType, implementationType, lifetime));
                else if (_behaviorInterfaces.Contains(definition))
                    services.TryAddEnumerable(new ServiceDescriptor(serviceType, implementationType, lifetime));
            }
        }
    }
}
