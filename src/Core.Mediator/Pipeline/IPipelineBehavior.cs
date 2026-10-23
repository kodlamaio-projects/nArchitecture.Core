namespace NArchitecture.Core.Mediator;

/// <summary>
/// Wraps the handling of a request. Behaviors run in registration order; each decides whether to call <c>next</c>.
/// Open generic behaviors may constrain <typeparamref name="TRequest"/> with marker interfaces; the container only
/// applies them to requests that satisfy the constraints.
/// </summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : notnull
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
