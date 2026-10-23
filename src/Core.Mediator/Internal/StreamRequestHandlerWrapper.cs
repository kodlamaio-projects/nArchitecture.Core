using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator.Exceptions;

namespace NArchitecture.Core.Mediator.Internal;

internal abstract class StreamRequestHandlerBase { }

internal abstract class StreamRequestHandlerWrapper<TResponse> : StreamRequestHandlerBase
{
    public abstract IAsyncEnumerable<TResponse> Handle(
        IStreamRequest<TResponse> request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken
    );
}

internal sealed class StreamRequestHandlerWrapperImpl<TRequest, TResponse> : StreamRequestHandlerWrapper<TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public override async IAsyncEnumerable<TResponse> Handle(
        IStreamRequest<TResponse> request,
        IServiceProvider serviceProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        IStreamRequestHandler<TRequest, TResponse> handler =
            serviceProvider.GetService<IStreamRequestHandler<TRequest, TResponse>>()
            ?? throw new HandlerNotFoundException(typeof(TRequest), typeof(IStreamRequestHandler<TRequest, TResponse>));

        TRequest typedRequest = (TRequest)request;

        StreamHandlerDelegate<TResponse> pipeline = () => handler.Handle(typedRequest, cancellationToken);

        IStreamPipelineBehavior<TRequest, TResponse>[] behaviors = serviceProvider
            .GetServices<IStreamPipelineBehavior<TRequest, TResponse>>()
            .ToArray();

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            IStreamPipelineBehavior<TRequest, TResponse> behavior = behaviors[i];
            StreamHandlerDelegate<TResponse> next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
        }

        await foreach (TResponse item in pipeline().WithCancellation(cancellationToken).ConfigureAwait(false))
            yield return item;
    }
}
