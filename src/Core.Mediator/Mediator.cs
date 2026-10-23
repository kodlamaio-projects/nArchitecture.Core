using System.Collections.Concurrent;
using NArchitecture.Core.Mediator.Internal;

namespace NArchitecture.Core.Mediator;

/// <summary>Default <see cref="IMediator"/>. Resolves handlers and behaviors from an <see cref="IServiceProvider"/>.</summary>
public sealed class Mediator : IMediator
{
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _requestHandlers = new();
    private static readonly ConcurrentDictionary<Type, StreamRequestHandlerBase> _streamRequestHandlers = new();

    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        RequestHandlerBase handler = _requestHandlers.GetOrAdd(
            request.GetType(),
            static (requestType, responseType) =>
                (RequestHandlerBase)
                    Activator.CreateInstance(typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType))!,
            typeof(TResponse)
        );

        return ((RequestHandlerWrapper<TResponse>)handler).Handle(request, _serviceProvider, cancellationToken);
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default) => Send<Unit>(request, cancellationToken);

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        StreamRequestHandlerBase handler = _streamRequestHandlers.GetOrAdd(
            request.GetType(),
            static (requestType, responseType) =>
                (StreamRequestHandlerBase)
                    Activator.CreateInstance(
                        typeof(StreamRequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType)
                    )!,
            typeof(TResponse)
        );

        return ((StreamRequestHandlerWrapper<TResponse>)handler).Handle(request, _serviceProvider, cancellationToken);
    }
}
