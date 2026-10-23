using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator.Exceptions;

namespace NArchitecture.Core.Mediator.Internal;

internal abstract class RequestHandlerBase { }

internal abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    public abstract Task<TResponse> Handle(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken
    );
}

internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(
        IRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken
    )
    {
        IRequestHandler<TRequest, TResponse> handler =
            serviceProvider.GetService<IRequestHandler<TRequest, TResponse>>()
            ?? throw new HandlerNotFoundException(typeof(TRequest), typeof(IRequestHandler<TRequest, TResponse>));

        TRequest typedRequest = (TRequest)request;

        RequestHandlerDelegate<TResponse> pipeline = token =>
            handler.Handle(typedRequest, resolveToken(token, cancellationToken));

        // Behaviors are resolved as IEnumerable so that constrained open generics the request does not satisfy are skipped.
        IPipelineBehavior<TRequest, TResponse>[] behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .ToArray();

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            IPipelineBehavior<TRequest, TResponse> behavior = behaviors[i];
            RequestHandlerDelegate<TResponse> next = pipeline;
            pipeline = token => behavior.Handle(typedRequest, next, resolveToken(token, cancellationToken));
        }

        return pipeline(cancellationToken);
    }

    private static CancellationToken resolveToken(CancellationToken provided, CancellationToken outer) =>
        provided == default ? outer : provided;
}
