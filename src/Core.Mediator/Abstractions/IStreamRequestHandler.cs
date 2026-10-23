namespace NArchitecture.Core.Mediator;

/// <summary>Handles an <see cref="IStreamRequest{TResponse}"/> by yielding items asynchronously.</summary>
public interface IStreamRequestHandler<in TRequest, out TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
