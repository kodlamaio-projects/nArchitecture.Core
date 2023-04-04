using Core.Persistence.Repositories;

namespace Core.Security.Entities;

public class RefreshToken : Entity<int>
{
    public int UserId { get; set; }
    public string Token { get; set; }
    public DateTime Expires { get; set; }
    public string CreatedByIp { get; set; }
    public DateTime? Revoked { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }

    public string? ReasonRevoked { get; set; }

    //public bool IsExpired => DateTime.UtcNow >= Expires;
    //public bool IsRevoked => Revoked != null;
    //public bool IsActive => !IsRevoked && !IsExpired;

    public virtual User User { get; set; }

    public RefreshToken() { }

    public RefreshToken(
        int id,
        string token,
        DateTime expires,
        DateTime createdDate,
        string createdByIp,
        DateTime? revoked,
        string revokedByIp,
        string replacedByToken,
        string reasonRevoked
    )
    {
        Id = id;
        Token = token;
        Expires = expires;
        CreatedDate = createdDate;
        CreatedByIp = createdByIp;
        Revoked = revoked;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
        ReasonRevoked = reasonRevoked;
    }
}
