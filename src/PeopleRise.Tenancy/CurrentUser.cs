namespace PeopleRise.Tenancy;

/// <summary>Per-request current user. In this starter it is set from a dev header; replace with real auth.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    void Set(Guid id);
}

public sealed class CurrentUser : ICurrentUser
{
    private Guid? _id;
    public bool IsAuthenticated => _id.HasValue;
    public Guid UserId => _id ?? throw new InvalidOperationException("No authenticated user on this request.");
    public void Set(Guid id) => _id = id;
}
