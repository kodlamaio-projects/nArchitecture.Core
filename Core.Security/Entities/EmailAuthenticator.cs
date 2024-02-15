using NArchitecture.Core.Persistence.Repositories;

namespace NArchitecture.Core.Security.Entities;

public class EmailAuthenticator<TUserId> : Entity<TUserId>
{
    public TUserId UserId { get; set; }
    public string? ActivationKey { get; set; }
    public bool IsVerified { get; set; }

    public EmailAuthenticator()
    {
        UserId = default!;
    }

    public EmailAuthenticator(TUserId userId, bool isVerified)
    {
        UserId = userId;
        IsVerified = isVerified;
    }

    public EmailAuthenticator(TUserId id, TUserId userId, bool isVerified)
        : base(id)
    {
        UserId = userId;
        IsVerified = isVerified;
    }
}
