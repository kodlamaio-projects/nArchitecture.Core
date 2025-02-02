using System.Security.Authentication;
using MediatR;
using Moq;
using NArchitecture.Core.Application.Pipelines.Authorization;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

namespace NArchitecture.Core.Application.Tests.Pipelines.Authorization;

public class AuthorizationBehaviorTests
{
    [Fact]
    public async Task Handle_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var request = new ValidSecuredRequest();
        request.IdentityRoles = ["admin", "user"];
        var next = Mock.Of<RequestHandlerDelegate<int>>();

        // Act
        _ = await new AuthorizationBehavior<ValidSecuredRequest, int>().Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(true);
    }

    public class ValidSecuredRequest : IRequest<int>, ISecuredRequest
    {
        public IEnumerable<string> IdentityRoles { get; set; } = [];
        public ReadOnlySpan<char> RequiredRoleClaims => "".AsSpan();
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsAuthorizationException()
    {
        // Arrange
        var request = new InvalidSecuredRequest();
        var next = Mock.Of<RequestHandlerDelegate<int>>();

        // Act and Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => new AuthorizationBehavior<InvalidSecuredRequest, int>().Handle(request, next, CancellationToken.None)
        );
    }

    public class InvalidSecuredRequest : IRequest<int>, ISecuredRequest
    {
        public IEnumerable<string> IdentityRoles { get; set; } = [];
        public ReadOnlySpan<char> RequiredRoleClaims => "".AsSpan();
    }

    [Fact]
    public async Task Handle_InvalidRequest_WithRequiredRoleClaims_ThrowsAuthorizationException()
    {
        // Arrange
        var request = new SecuredRequestWithRequiredRoleClaims();
        request.IdentityRoles = new[] { "user" };
        var next = Mock.Of<RequestHandlerDelegate<int>>();

        // Act and Assert
        await Assert.ThrowsAsync<AuthorizationException>(
            () =>
                new AuthorizationBehavior<SecuredRequestWithRequiredRoleClaims, int>().Handle(
                    request,
                    next,
                    CancellationToken.None
                )
        );
    }

    public class SecuredRequestWithRequiredRoleClaims : IRequest<int>, ISecuredRequest
    {
        public IEnumerable<string> IdentityRoles { get; set; } = [];
        public ReadOnlySpan<char> RequiredRoleClaims => "admin".AsSpan();
    }

    [Fact]
    public async Task Handle_ValidRequest_WithRequiredRoleClaims_ReturnsResponse()
    {
        // Arrange
        var request = new SecuredRequestWithoutRequiredRoleClaims();
        request.IdentityRoles = new[] { "user", "admin" };
        var next = Mock.Of<RequestHandlerDelegate<int>>();

        // Act
        _ = await new AuthorizationBehavior<SecuredRequestWithoutRequiredRoleClaims, int>().Handle(
            request,
            next,
            CancellationToken.None
        );

        // Assert
        Assert.True(true);
    }

    public class SecuredRequestWithoutRequiredRoleClaims : IRequest<int>, ISecuredRequest
    {
        public IEnumerable<string> IdentityRoles { get; set; } = [];
        public ReadOnlySpan<char> RequiredRoleClaims => "editor,admin".AsSpan();
    }
}
