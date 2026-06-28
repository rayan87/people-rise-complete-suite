namespace PeopleRise.Tenancy;

/// <summary>Builds a per-tenant connection string from a template + the tenant's database name.</summary>
public sealed class TenantConnectionFactory(string template)
{
    public string ForDatabase(string dbName)
    {
        var t = template.TrimEnd();
        var sep = t.EndsWith(';') ? "" : ";";
        return $"{t}{sep}Database={dbName}";
    }
}
