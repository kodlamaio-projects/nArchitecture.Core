using NArchitecture.Core.Persistence.Repositories;

namespace NArchitecture.Core.Security.Entities;

public class RefreshToken<TUserId, TOperationClaimId> : Entity<TUserId>
{
    public TUserId UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresDate { get; set; }
    public string CreatedByIp { get; set; }
    public DateTime? RevokedDate { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? ReasonRevoked { get; set; }

    public virtual User<TUserId, TOperationClaimId> User { get; set; } = null!;

    public RefreshToken()
    {
        UserId = default!;
        Token = string.Empty;
        CreatedByIp = string.Empty;
    }

    public RefreshToken(TUserId userId, string token, DateTime expiresDate, string createdByIp)
    {
        UserId = userId;
        Token = token;
        ExpiresDate = expiresDate;
        CreatedByIp = createdByIp;
    }

    public RefreshToken(TUserId id, TUserId userId, string token, DateTime expiresDate, string createdByIp)
        : base(id)
    {
        UserId = userId;
        Token = token;
        ExpiresDate = expiresDate;
        CreatedByIp = createdByIp;
    }
}
