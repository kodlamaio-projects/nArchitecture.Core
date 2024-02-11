using NArchitecture.Core.Persistence.Repositories;
using NArchitecture.Core.Security.Enums;

namespace NArchitecture.Core.Security.Entities;

public class User<TId, TOperationClaimId> : Entity<TId>
{
    public string Email { get; set; }
    public byte[] PasswordSalt { get; set; }
    public byte[] PasswordHash { get; set; }
    public AuthenticatorType AuthenticatorType { get; set; }

    public virtual ICollection<UserOperationClaim<TId, TOperationClaimId>> UserOperationClaims { get; set; } = null!;
    public virtual ICollection<RefreshToken<TId, TOperationClaimId>> RefreshTokens { get; set; } = null!;
    public virtual ICollection<EmailAuthenticator<TId, TOperationClaimId>> EmailAuthenticators { get; set; } = null!;
    public virtual ICollection<OtpAuthenticator<TId, TOperationClaimId>> OtpAuthenticators { get; set; } = null!;

    public User()
    {
        Email = string.Empty;
        PasswordHash = Array.Empty<byte>();
        PasswordSalt = Array.Empty<byte>();
    }

    public User(string email, byte[] passwordSalt, byte[] passwordHash, AuthenticatorType authenticatorType)
    {
        Email = email;
        PasswordSalt = passwordSalt;
        PasswordHash = passwordHash;
        AuthenticatorType = authenticatorType;
    }

    public User(TId id, string email, byte[] passwordSalt, byte[] passwordHash, AuthenticatorType authenticatorType)
        : base(id)
    {
        Email = email;
        PasswordSalt = passwordSalt;
        PasswordHash = passwordHash;
        AuthenticatorType = authenticatorType;
    }
}
