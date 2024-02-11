using System.Collections.Immutable;
using System.Security.Claims;

namespace NArchitecture.Core.Security.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static ICollection<string>? Claims(this ClaimsPrincipal claimsPrincipal, string claimType)
    {
        return claimsPrincipal?.FindAll(claimType)?.Select(x => x.Value).ToImmutableArray();
    }

    public static ICollection<string>? ClaimRoles(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.Claims(ClaimTypes.Role);
    }

    public static int GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        string? result = claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Convert.ToInt32(result);
    }
}
