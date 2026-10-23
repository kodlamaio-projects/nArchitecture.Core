namespace NArchitecture.Core.Mediator;

/// <summary>
/// Continuation to the next pipeline behavior or the handler itself.
/// Calling it without a token (<c>await next()</c>) reuses the token the request was sent with.
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);
