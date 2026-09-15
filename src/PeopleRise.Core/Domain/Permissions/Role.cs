using System.ComponentModel.DataAnnotations.Schema;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>A tenant-configured role (Core Spec §3.6): "roles configured by the customer... a
/// tenant administrator defines roles and grants permissions to them in settings." Nothing
/// hardcoded - no built-in role names. Permissions are stored as a delimited string, the same
/// convention Organization uses for its small multi-valued fields (WeekendDays,
/// SupportedLanguages) - a child table would be more normalized but this is simpler and this list
/// is always read/written as a whole, never queried by individual permission.</summary>
internal class Role : Entity
{
    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public string PermissionsCsv { get; private set; } = "";   // comma-delimited Permission names

    public RoleStatus Status { get; private set; } = RoleStatus.Active;

    [NotMapped]
    public IReadOnlySet<Permission> Permissions => ParseCsv(PermissionsCsv);

    private Role() { }   // EF

    public static Role Create(string nameEn, string? nameAr, IEnumerable<Permission> permissions)
    {
        var role = new Role { NameEn = nameEn, NameAr = nameAr };
        role.SetPermissions(permissions);
        return role;
    }

    public void Update(string nameEn, string? nameAr, IEnumerable<Permission> permissions)
    {
        NameEn = nameEn;
        NameAr = nameAr;
        SetPermissions(permissions);
    }

    public void SetPermissions(IEnumerable<Permission> permissions) =>
        PermissionsCsv = string.Join(",", permissions.Distinct());

    /// <summary>Closed, never deleted, once a user has ever been assigned it (Core Spec §3.4).</summary>
    public void Close() => Status = RoleStatus.Closed;

    private static IReadOnlySet<Permission> ParseCsv(string csv) =>
        string.IsNullOrEmpty(csv)
            ? new HashSet<Permission>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<Permission>).ToHashSet();
}

public enum RoleStatus { Active, Closed }

/// <summary>The tenant-configurable permission vocabulary (Core Spec §3.6). ViewSensitiveData and
/// ManageSensitiveData cover the ONE sensitivity class named in §3.5 as a single unit: pay amounts,
/// compa-ratio, held competency profiles, assessment results, and burnout signals. A role granted
/// either of these two is deliberately granted access to all of them - never to just pay, or just
/// held profiles, as separate concerns.</summary>
public enum Permission
{
    ManageOrganization,
    ManageStructure,
    ManageLadder,
    ManageEstablishment,
    ManageRoster,
    ManagePermissions,
    ViewSensitiveData,
    ManageSensitiveData,
}
