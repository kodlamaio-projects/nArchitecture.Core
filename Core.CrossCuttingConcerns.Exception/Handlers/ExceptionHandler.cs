using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

namespace NArchitecture.Core.CrossCuttingConcerns.Exception.Handlers;

public abstract class ExceptionHandler
{
    public abstract Task HandleBusinessException(BusinessException businessException);
    public abstract Task HandleValidationException(ValidationException validationException);
    public abstract Task HandleAuthorizationException(AuthorizationException authorizationException);
    public abstract Task HandleNotFoundException(NotFoundException notFoundException);
    public abstract Task HandleException(System.Exception exception);
}
