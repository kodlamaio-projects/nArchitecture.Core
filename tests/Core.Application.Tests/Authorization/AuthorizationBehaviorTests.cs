using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NArchitecture.Core.Application.Pipelines.Authorization;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Mediator;
using NArchitecture.Core.Security.Constants;

namespace Core.Application.Tests.Authorization;

public class AuthorizationBehaviorTests
{
    private sealed record SecuredQuery(params string[] Roles) : IRequest<string>, ISecuredRequest
    {
        string[] ISecuredRequest.Roles => Roles;
    }

    private static IHttpContextAccessor accessor(params Claim[] claims)
    {
        DefaultHttpContext context = new() { User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test")) };
        return new HttpContextAccessor { HttpContext = context };
    }

    private static Task<string> next(CancellationToken _ = default) => Task.FromResult("ok");

    [Fact]
    public async Task Null_HttpContext_is_not_authenticated_rather_than_a_null_reference()
    {
        AuthorizationBehavior<SecuredQuery, string> behavior = new(new HttpContextAccessor { HttpContext = null });

        AuthorizationException ex = await Assert.ThrowsAsync<AuthorizationException>(() =>
            behavior.Handle(new SecuredQuery("Users.Read"), next, TestContext.Current.CancellationToken)
        );

        Assert.Equal("You are not authenticated.", ex.Message);
    }

    [Fact]
    public async Task User_without_claims_is_not_authenticated()
    {
        AuthorizationBehavior<SecuredQuery, string> behavior = new(accessor());

        AuthorizationException ex = await Assert.ThrowsAsync<AuthorizationException>(() =>
            behavior.Handle(new SecuredQuery("Users.Read"), next, TestContext.Current.CancellationToken)
        );

        Assert.Equal("You are not authenticated.", ex.Message);
    }

    [Fact]
    public async Task User_without_matching_role_is_not_authorized()
    {
        AuthorizationBehavior<SecuredQuery, string> behavior = new(accessor(new Claim(ClaimTypes.Role, "Users.Write")));

        AuthorizationException ex = await Assert.ThrowsAsync<AuthorizationException>(() =>
            behavior.Handle(new SecuredQuery("Users.Read"), next, TestContext.Current.CancellationToken)
        );

        Assert.Equal("You are not authorized.", ex.Message);
    }

    [Fact]
    public async Task Matching_role_passes()
    {
        AuthorizationBehavior<SecuredQuery, string> behavior = new(accessor(new Claim(ClaimTypes.Role, "Users.Read")));

        Assert.Equal("ok", await behavior.Handle(new SecuredQuery("Users.Read"), next, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Admin_passes_regardless_of_requested_roles()
    {
        AuthorizationBehavior<SecuredQuery, string> behavior = new(
            accessor(new Claim(ClaimTypes.Role, GeneralOperationClaims.Admin))
        );

        Assert.Equal("ok", await behavior.Handle(new SecuredQuery("Anything"), next, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Authenticated_user_passes_when_request_requires_no_roles()
    {
        AuthorizationBehavior<SecuredQuery, string> behavior = new(accessor(new Claim(ClaimTypes.Name, "halit")));

        Assert.Equal("ok", await behavior.Handle(new SecuredQuery(), next, TestContext.Current.CancellationToken));
    }
}
