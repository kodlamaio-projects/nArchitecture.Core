namespace NArchitecture.Core.Application.Pipelines.Authorization;

public interface ISecuredRequest
{
    public IEnumerable<string> IdentityRoles { get; set; }
    public ReadOnlySpan<char> RequiredRoleClaims { get; }
}
