using MediatR;
using Microsoft.AspNetCore.Http;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Security.Constants;
using NArchitecture.Core.Security.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NArchitecture.Core.Application.Pipelines.Authorization
{
    public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>, IPipelineBehavior<TRequest, IAsyncEnumerable<TResponse>>
        where TRequest : ISecuredRequest
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
            CheckAuthorization(request);
            return await next();
        }

        public async Task<IAsyncEnumerable<TResponse>> Handle(
            TRequest request,
            RequestHandlerDelegate<IAsyncEnumerable<TResponse>> next,
            CancellationToken cancellationToken
        )
        {
            CheckAuthorization(request);

            return await next();
        }

        private void CheckAuthorization(TRequest request)
        {
            if (!_httpContextAccessor.HttpContext.User.Claims.Any())
                throw new AuthorizationException("You are not authenticated.");

            if (request.Roles.Any())
            {
                string[] userRoleClaims = _httpContextAccessor.HttpContext.User.GetRoleClaims()?.ToArray() ?? Array.Empty<string>();
                bool isMatchedAUserRoleClaimWithRequestRoles = userRoleClaims.Any(userRoleClaim =>
                    userRoleClaim == GeneralOperationClaims.Admin || request.Roles.Contains(userRoleClaim)
                );

                if (!isMatchedAUserRoleClaimWithRequestRoles)
                    throw new AuthorizationException("You are not authorized.");
            }
        }
    }
}
