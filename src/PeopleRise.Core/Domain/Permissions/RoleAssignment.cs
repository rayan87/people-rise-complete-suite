using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>Grants a control-plane user a tenant-defined Role (Core Spec §3.6: "the control plane
/// says who may reach a tenant; the tenant database says what they may do inside it"). UserId is a
/// bare id, not an FK - AppUser lives in the control-plane database, a different database entirely
/// (LOCKED RULE 1: DB-per-tenant, no cross-database FKs).</summary>
internal class RoleAssignment : Entity
{
    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public Role? Role { get; private set; }

    private RoleAssignment() { }   // EF

    public static RoleAssignment Create(Guid userId, Guid roleId) => new() { UserId = userId, RoleId = roleId };
}
