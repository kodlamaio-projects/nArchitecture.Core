namespace NArchitecture.Core.Mediator;

/// <summary>Wraps the handling of a stream request.</summary>
public interface IStreamPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    IAsyncEnumerable<TResponse> Handle(
        TRequest request,
        StreamHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    );
}
