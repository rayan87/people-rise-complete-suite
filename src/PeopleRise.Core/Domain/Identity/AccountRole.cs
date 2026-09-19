using Microsoft.AspNetCore.Identity;

namespace PeopleRise.Core.Domain;

/// <summary>A tenant-configured role (Core Spec §11.2): "a tenant administrator defines roles and
/// grants permissions to them in settings." Nothing hardcoded - no built-in role names except the
/// seeded, system-owned Admin role (see <see cref="IsSystemOwned"/>). Permissions are granted as
/// ASP.NET Identity role claims (type <see cref="PermissionClaimType"/>) rather than a bespoke join
/// table - RoleManager&lt;AccountRole&gt; already has AddClaimAsync/GetClaimsAsync/RemoveClaimAsync.</summary>
internal class AccountRole : IdentityRole<Guid>
{
    public const string PermissionClaimType = "permission";

    public AccountRole() => Id = Guid.CreateVersion7();

    public string? NameAr { get; private set; }

    /// <summary>The seeded Admin role (§11.2): "exists from provisioning with the full tenant
    /// permission set. Its permission set cannot be edited and the role cannot be deleted... Its
    /// display label is editable, so the naming freedom above still holds."</summary>
    public bool IsSystemOwned { get; private set; }

    public RoleStatus Status { get; private set; } = RoleStatus.Active;

    public static AccountRole Create(string name, string? nameAr, bool isSystemOwned = false) => new()
    {
        Name = name,
        NameAr = nameAr,
        IsSystemOwned = isSystemOwned,
    };

    public void Rename(string name, string? nameAr)
    {
        Name = name;
        NameAr = nameAr;
    }

    /// <summary>Closed, never deleted, once assigned to at least one account (Core Spec §3.4).</summary>
    public void Close() => Status = RoleStatus.Closed;
}

public enum RoleStatus 
{ 
    Active, 
    Closed 
}
