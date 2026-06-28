namespace PeopleRise.SharedKernel;

/// <summary>Mutable aggregate/entity base. UUIDv7 keys are time-ordered for index locality.</summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Insert-only base. Rows are never updated or deleted (e.g. evaluation answers, audit rows).</summary>
public abstract class ImmutableEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; }
}
