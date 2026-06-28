namespace PeopleRise.Tenancy;

/// <summary>Per-request holder of the resolved tenant + its connection string. Scoped service.</summary>
public interface ITenantContext
{
    bool IsResolved { get; }
    Guid TenantId { get; }
    string ConnectionString { get; }
    void Set(Guid tenantId, string connectionString);
}

public sealed class TenantContext : ITenantContext
{
    private Guid? _id;
    private string? _cs;
    public bool IsResolved => _cs is not null;
    public Guid TenantId => _id ?? throw new InvalidOperationException("No tenant resolved for this request.");
    public string ConnectionString => _cs
        ?? throw new InvalidOperationException("No tenant resolved. Provide the X-Tenant-Id header for tenant-scoped calls.");
    public void Set(Guid tenantId, string connectionString) { _id = tenantId; _cs = connectionString; }
}
