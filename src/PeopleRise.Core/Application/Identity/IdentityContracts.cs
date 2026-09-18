using PeopleRise.Core.Domain;

namespace PeopleRise.Core.Application.Permissions;

public record RoleDto(Guid Id, string NameEn, string? NameAr, IReadOnlyList<string> Permissions, string Status);
public record CreateRoleRequest(string NameEn, string? NameAr, IReadOnlyList<string> Permissions);
public record UpdateRoleRequest(string NameEn, string? NameAr, IReadOnlyList<string> Permissions);

public record RoleAssignmentDto(Guid Id, Guid UserId, Guid RoleId, string RoleNameEn);
public record AssignRoleRequest(Guid UserId, Guid RoleId);

/// <summary>The full permission vocabulary, so a Core UI settings screen can render the grant
/// checkboxes without hardcoding the list on the frontend too.</summary>
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<string> All = Enum.GetNames<Permission>();
}
