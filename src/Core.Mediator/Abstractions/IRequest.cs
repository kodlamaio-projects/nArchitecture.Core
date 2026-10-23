namespace NArchitecture.Core.Mediator;

/// <summary>A request that produces a <typeparamref name="TResponse"/> when handled.</summary>
public interface IRequest<out TResponse> : IBaseRequest { }

/// <summary>A request without a meaningful response. Handled through <see cref="IRequestHandler{TRequest}"/>.</summary>
public interface IRequest : IRequest<Unit> { }
