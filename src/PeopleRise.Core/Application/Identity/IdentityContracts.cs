using Microsoft.AspNetCore.Identity;
using PeopleRise.Core.Domain;

namespace PeopleRise.Core.Application.Identity;

public record AccountDto(Guid Id, string Email, Guid? EmployeeId, string Status);
public record CreateAccountRequest(string Email, string Password, Guid? EmployeeId);

public record RoleDto(Guid Id, string Name, string? NameAr, IReadOnlyList<string> Permissions, bool IsSystemOwned, string Status);
public record CreateRoleRequest(string Name, string? NameAr, IReadOnlyList<string> Permissions);
public record UpdateRoleRequest(string Name, string? NameAr, IReadOnlyList<string> Permissions);

public record AssignRoleRequest(Guid AccountId, Guid RoleId);

public static class PermissionCatalog
{
    public static readonly IReadOnlyList<string> All = Enum.GetNames<Permission>();
}

internal static class IdentityMapping
{
    public static AccountDto ToDto(this Account account) =>
        new(account.Id, account.Email!, account.EmployeeId, account.Status.ToString());

    public static async Task<RoleDto> ToDtoAsync(this AccountRole role, RoleManager<AccountRole> roles)
    {
        var claims = await roles.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == AccountRole.PermissionClaimType)
            .Select(c => c.Value)
            .ToList();
        return new RoleDto(role.Id, role.Name!, role.NameAr, permissions, role.IsSystemOwned, role.Status.ToString());
    }
}
