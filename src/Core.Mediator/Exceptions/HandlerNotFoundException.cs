namespace NArchitecture.Core.Mediator.Exceptions;

/// <summary>Thrown when no handler is registered for a request type.</summary>
public sealed class HandlerNotFoundException : InvalidOperationException
{
    public Type RequestType { get; }
    public Type HandlerType { get; }

    public HandlerNotFoundException(Type requestType, Type handlerType)
        : base(
            $"No handler is registered for request '{requestType.FullName}'. Register an implementation of "
                + $"'{handlerType.FullName}', for example via AddMediator(cfg => cfg.RegisterServicesFromAssemblyContaining<{requestType.Name}>())."
        )
    {
        RequestType = requestType;
        HandlerType = handlerType;
    }
}
