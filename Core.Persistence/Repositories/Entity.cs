namespace Core.Persistence.Repositories;

public class Entity<TId>
{
    public TId Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public Entity() { }

    public Entity(TId id)
        : this()
    {
        Id = id;
    }
}
