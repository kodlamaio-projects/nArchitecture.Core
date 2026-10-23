using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NArchitecture.Core.Mediator.DependencyInjection;

/// <summary>Options collected by <see cref="MediatorServiceCollectionExtensions.AddMediator(IServiceCollection, Action{MediatorConfiguration})"/>.</summary>
public sealed class MediatorConfiguration
{
    internal List<Assembly> AssembliesToScan { get; } = [];

    /// <summary>Behavior registrations in the order they were added. Order is preserved at runtime.</summary>
    internal List<ServiceDescriptor> BehaviorDescriptors { get; } = [];

    /// <summary>Lifetime for <see cref="IMediator"/>, handlers and behaviors unless a behavior overrides it. Default: Transient.</summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    public MediatorConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (!AssembliesToScan.Contains(assembly))
            AssembliesToScan.Add(assembly);
        return this;
    }

    public MediatorConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        foreach (Assembly assembly in assemblies)
            RegisterServicesFromAssembly(assembly);
        return this;
    }

    public MediatorConfiguration RegisterServicesFromAssemblyContaining<T>() => RegisterServicesFromAssembly(typeof(T).Assembly);

    public MediatorConfiguration RegisterServicesFromAssemblyContaining(Type type) => RegisterServicesFromAssembly(type.Assembly);

    /// <summary>Registers a closed behavior for every <see cref="IPipelineBehavior{TRequest,TResponse}"/> it implements.</summary>
    public MediatorConfiguration AddBehavior<TBehavior>(ServiceLifetime? lifetime = null)
        where TBehavior : class => AddBehavior(typeof(TBehavior), lifetime);

    public MediatorConfiguration AddBehavior(Type behaviorType, ServiceLifetime? lifetime = null)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);
        Type[] serviceTypes = behaviorType
            .GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToArray();
        if (serviceTypes.Length == 0)
            throw new ArgumentException(
                $"{behaviorType.FullName} does not implement {typeof(IPipelineBehavior<,>).FullName}.",
                nameof(behaviorType)
            );

        foreach (Type serviceType in serviceTypes)
            AddBehavior(serviceType, behaviorType, lifetime);
        return this;
    }

    public MediatorConfiguration AddBehavior(Type serviceType, Type implementationType, ServiceLifetime? lifetime = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);
        addDescriptor(new ServiceDescriptor(serviceType, implementationType, lifetime ?? Lifetime));
        return this;
    }

    /// <summary>
    /// Registers an open generic behavior such as <c>typeof(LoggingBehavior&lt;,&gt;)</c>.
    /// Generic constraints on the behavior are honored by the container: it only applies to requests that satisfy them.
    /// </summary>
    public MediatorConfiguration AddOpenBehavior(Type openBehaviorType, ServiceLifetime? lifetime = null) =>
        addOpenBehavior(openBehaviorType, typeof(IPipelineBehavior<,>), lifetime);

    /// <summary>Registers an open generic stream behavior such as <c>typeof(MyStreamBehavior&lt;,&gt;)</c>.</summary>
    public MediatorConfiguration AddOpenStreamBehavior(Type openBehaviorType, ServiceLifetime? lifetime = null) =>
        addOpenBehavior(openBehaviorType, typeof(IStreamPipelineBehavior<,>), lifetime);

    private MediatorConfiguration addOpenBehavior(Type openBehaviorType, Type openServiceType, ServiceLifetime? lifetime)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);
        if (!openBehaviorType.IsGenericTypeDefinition)
            throw new ArgumentException(
                $"{openBehaviorType.FullName} must be an open generic type definition, e.g. typeof(MyBehavior<,>).",
                nameof(openBehaviorType)
            );

        bool implementsService = openBehaviorType
            .GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openServiceType);
        if (!implementsService)
            throw new ArgumentException(
                $"{openBehaviorType.FullName} does not implement {openServiceType.FullName}.",
                nameof(openBehaviorType)
            );

        addDescriptor(new ServiceDescriptor(openServiceType, openBehaviorType, lifetime ?? Lifetime));
        return this;
    }

    private void addDescriptor(ServiceDescriptor descriptor)
    {
        bool alreadyAdded = BehaviorDescriptors.Any(d =>
            d.ServiceType == descriptor.ServiceType && d.ImplementationType == descriptor.ImplementationType
        );
        if (!alreadyAdded)
            BehaviorDescriptors.Add(descriptor);
    }
}
