using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Mediator;
using NArchitecture.Core.Security.Constants;
using NArchitecture.Core.Security.Extensions;

namespace NArchitecture.Core.Application.Pipelines.Authorization;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ISecuredRequest
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationBehavior(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user is null || !user.Claims.Any())
            throw new AuthorizationException("You are not authenticated.");

        if (request.Roles.Length > 0)
        {
            ICollection<string> userRoleClaims = user.GetRoleClaims() ?? [];
            bool isNotMatchedAUserRoleClaimWithRequestRoles = !userRoleClaims.Any(userRoleClaim =>
                userRoleClaim == GeneralOperationClaims.Admin || request.Roles.Contains(userRoleClaim)
            );
            if (isNotMatchedAUserRoleClaimWithRequestRoles)
                throw new AuthorizationException("You are not authorized.");
        }

        TResponse response = await next();
        return response;
    }
}
