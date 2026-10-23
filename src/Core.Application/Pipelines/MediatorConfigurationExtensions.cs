using NArchitecture.Core.Application.Pipelines.Authorization;
using NArchitecture.Core.Application.Pipelines.Caching;
using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Application.Pipelines.Transaction;
using NArchitecture.Core.Application.Pipelines.Validation;
using NArchitecture.Core.Mediator.DependencyInjection;

namespace NArchitecture.Core.Application.Pipelines;

public static class MediatorConfigurationExtensions
{
    /// <summary>
    /// Registers the standard NArchitecture pipeline in its canonical order:
    /// authorization, caching, cache removing, logging, request validation, transaction scope.
    /// <c>PerformanceBehavior</c> is opt-in because it needs a <c>Stopwatch</c> registration.
    /// </summary>
    public static MediatorConfiguration AddNArchitecturePipelines(this MediatorConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        configuration.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
        configuration.AddOpenBehavior(typeof(CachingBehavior<,>));
        configuration.AddOpenBehavior(typeof(CacheRemovingBehavior<,>));
        configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
        configuration.AddOpenBehavior(typeof(RequestValidationBehavior<,>));
        configuration.AddOpenBehavior(typeof(TransactionScopeBehavior<,>));
        return configuration;
    }
}
