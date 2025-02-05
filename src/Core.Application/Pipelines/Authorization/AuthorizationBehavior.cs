using System.Security.Authentication;
using MediatR;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Security.Constants;

namespace NArchitecture.Core.Application.Pipelines.Authorization;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ISecuredRequest
{
    public virtual async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        // Is Authenticated
        if (request.IdentityRoles == null)
            throwAuthenticationException();

        // Is Authorized
        if (!request.RequiredRoleClaims.IsEmpty)
        {
            if (!request.IdentityRoles!.GetEnumerator().MoveNext())
                throwAuthorizationException();

            if (
                !(
                    request.IdentityRoles!.Contains(GeneralOperationClaims.Admin)
                    || isHasRequiredRole(request.IdentityRoles, request.RequiredRoleClaims)
                )
            )
                throwAuthorizationException();
        }

        return await next();
    }

    private bool isHasRequiredRole(IEnumerable<string> identityRoles, ReadOnlySpan<char> requiredRoleClaims)
    {
        bool isMatch = false;
        foreach (var role in identityRoles)
            for (int i = 0; i < requiredRoleClaims.Length; ++i)
            {
                if (requiredRoleClaims[i] == ',')
                    continue;

                if (requiredRoleClaims[i] == role[i])
                    isMatch = true;
                else
                {
                    isMatch = false;
                    break;
                }
            }

        return isMatch;
    }

    protected virtual void throwAuthenticationException()
    {
        throw new AuthenticationException("You are not authenticated.");
    }

    protected virtual void throwAuthorizationException()
    {
        throw new AuthorizationException("You are not authorized.");
    }
}
