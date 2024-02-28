using System.Security.Claims;

namespace NArchitecture.Core.Security.Extensions;

public static class ClaimExtensions
{
    public static void AddEmail(this ICollection<Claim> claims, string email)
    {
        claims.Add(new Claim(ClaimTypes.Email, email));
    }

    public static void AddName(this ICollection<Claim> claims, string name)
    {
        claims.Add(new Claim(ClaimTypes.Name, name));
    }

    public static void AddNameIdentifier(this ICollection<Claim> claims, string nameIdentifier)
    {
        claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
    }

    public static void AddRoles(this ICollection<Claim> claims, ICollection<string> roles)
    {
        foreach (string role in roles)
            claims.AddRole(role);
    }

    public static void AddRole(this ICollection<Claim> claims, string role)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }
}
