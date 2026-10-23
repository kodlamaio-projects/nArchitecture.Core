namespace NArchitecture.Core.Mediator;

/// <summary>A request whose handler yields a stream of <typeparamref name="TResponse"/> items.</summary>
public interface IStreamRequest<out TResponse> : IBaseRequest { }
