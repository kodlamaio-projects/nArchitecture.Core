using BenchmarkDotNet.Attributes;
using MediatR;
using NArchitecture.Core.Application.Pipelines.Authorization;
using NArchitecture.Core.Security.Constants;

namespace Core.Application.BenchmarkTests.Pipelines.Authorization;

[MemoryDiagnoser(true)]
public class AuthorizationBehaviorBenchmark
{
    public class ExampleRequestType : IRequest<ExampleResponseType>, ISecuredRequest
    {
        public IEnumerable<string> IdentityRoles { get; set; }
        private const string _requiredRoleClaims = "Test";
        public ReadOnlySpan<char> RequiredRoleClaims => _requiredRoleClaims.AsSpan();

        public class ExampleRequestTypeHandler : IRequestHandler<ExampleRequestType, ExampleResponseType>
        {
            public Task<ExampleResponseType?> Handle(ExampleRequestType request, CancellationToken cancellationToken)
            {
                return Task.FromResult(default(ExampleResponseType));
            }
        }
    }

    public class ExampleResponseType { }

    [Benchmark]
    public async Task<ExampleResponseType?> BenchmarkAuthorizationBehavior()
    {
        ExampleRequestType request = new() { IdentityRoles = [GeneralOperationClaims.Admin] };

        AuthorizationBehavior<ExampleRequestType, ExampleResponseType> authorizationBehavior =
            new AuthorizationBehavior<ExampleRequestType, ExampleResponseType>();
        return await authorizationBehavior.Handle(
            request,
            () => Task.FromResult(default(ExampleResponseType)),
            CancellationToken.None
        );
    }
}
