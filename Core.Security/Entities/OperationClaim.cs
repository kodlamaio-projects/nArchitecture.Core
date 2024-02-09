using Core.Persistence.Repositories;

namespace Core.Security.Entities;

public class OperationClaim<TId, TUserId> : Entity<TId>
{
    public string Name { get; set; }

    public virtual ICollection<UserOperationClaim<TUserId, TId>> UserOperationClaims { get; set; } = null!;

    public OperationClaim()
    {
        Name = string.Empty;
    }

    public OperationClaim(string name)
    {
        Name = name;
    }

    public OperationClaim(TId id, string name)
        : base(id)
    {
        Name = name;
    }
}
