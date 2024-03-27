namespace NArchitecture.Core.Persistence.Repositories;

public abstract class Entity<TId> : IEntity<TId>, IEntityTimestamps
{
    public TId Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? DeletedDate { get; set; }

    public Entity()
    {
        Id = default!;
    }

    public Entity(TId id)
    {
        Id = id;
    }
}
