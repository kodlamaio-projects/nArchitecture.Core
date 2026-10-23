using System.Runtime.CompilerServices;
using NArchitecture.Core.Mediator;

namespace Core.Mediator.Tests;

public sealed class Trace
{
    public List<string> Entries { get; } = [];
}

public sealed record Ping(string Message) : IRequest<string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult($"{request.Message} Pong");
}

public sealed record Fire : IRequest;

public sealed class FireHandler : IRequestHandler<Fire>
{
    public int Calls { get; private set; }

    public Task Handle(Fire request, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.CompletedTask;
    }
}

public sealed record Orphan : IRequest<int>;

public interface IMarker { }

public sealed record Marked(string Message) : IRequest<string>, IMarker;

public sealed class MarkedHandler : IRequestHandler<Marked, string>
{
    public Task<string> Handle(Marked request, CancellationToken cancellationToken) => Task.FromResult(request.Message);
}

public sealed record TokenProbe : IRequest<CancellationToken>;

public sealed class TokenProbeHandler : IRequestHandler<TokenProbe, CancellationToken>
{
    public Task<CancellationToken> Handle(TokenProbe request, CancellationToken cancellationToken) =>
        Task.FromResult(cancellationToken);
}

public sealed record CountUp(int Count) : IStreamRequest<int>;

public sealed class CountUpHandler : IStreamRequestHandler<CountUp, int>
{
    public async IAsyncEnumerable<int> Handle(CountUp request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (int i = 0; i < request.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return i;
        }
    }
}

public abstract class TracingBehavior<TRequest, TResponse>(Trace trace, string name) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        trace.Entries.Add($"{name}>");
        TResponse response = await next();
        trace.Entries.Add($"<{name}");
        return response;
    }
}

public sealed class BehaviorA<TRequest, TResponse>(Trace trace) : TracingBehavior<TRequest, TResponse>(trace, "A")
    where TRequest : notnull;

public sealed class BehaviorB<TRequest, TResponse>(Trace trace) : TracingBehavior<TRequest, TResponse>(trace, "B")
    where TRequest : notnull;

public sealed class BehaviorC<TRequest, TResponse>(Trace trace) : TracingBehavior<TRequest, TResponse>(trace, "C")
    where TRequest : notnull;

public sealed class MarkerOnlyBehavior<TRequest, TResponse>(Trace trace) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IMarker
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        trace.Entries.Add("Marker");
        return await next();
    }
}

public sealed class DoublingStreamBehavior<TRequest, TResponse>(Trace trace) : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async IAsyncEnumerable<TResponse> Handle(
        TRequest request,
        StreamHandlerDelegate<TResponse> next,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        trace.Entries.Add("Stream>");
        await foreach (TResponse item in next().WithCancellation(cancellationToken))
        {
            if (item is int i)
                yield return (TResponse)(object)(i * 2);
            else
                yield return item;
        }
        trace.Entries.Add("<Stream");
    }
}
