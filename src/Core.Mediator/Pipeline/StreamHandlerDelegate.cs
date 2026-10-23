namespace NArchitecture.Core.Mediator;

/// <summary>Continuation to the next stream behavior or the stream handler itself.</summary>
public delegate IAsyncEnumerable<TResponse> StreamHandlerDelegate<out TResponse>();
