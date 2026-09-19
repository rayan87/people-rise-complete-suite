namespace PeopleRise.Core.Domain;

/// <summary>The tenant-configurable permission vocabulary (Core Spec §11.2): "every product enforces
/// access through one permission model, and the core is where it lives." ViewSensitiveData and
/// ManageSensitiveData cover the ONE sensitivity class named in §3.5/§11.2 as a single unit: pay
/// amounts, compa-ratio, held competency profiles, assessment results, and burnout signals. A role
/// granted either of these two is deliberately granted access to all of them - never to just pay,
/// or just held profiles, as separate concerns.</summary>
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
