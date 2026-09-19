using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record UpdateRoleCommand(Guid Id, string Name, string? NameAr, IReadOnlyList<string> Permissions);

internal sealed class UpdateRoleHandler(RoleManager<AccountRole> roles)
    : ICommandHandler<UpdateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand cmd, CancellationToken ct)
    {
        var role = await roles.FindByIdAsync(cmd.Id.ToString());
        if (role is null) return Error.NotFound("Role not found.");
        if (string.IsNullOrWhiteSpace(cmd.Name)) return Error.Validation("Name is required.");

        if (!CreateRoleHandler.TryParsePermissions(cmd.Permissions, out var parsed, out var error))
        {
            return Error.Validation(error!);
        }

        // Core Spec §11.2: the seeded Admin role's permission set cannot be edited - its display
        // label still can be, so only reject when the caller actually tried to change permissions.
        if (role.IsSystemOwned)
        {
            var current = (await roles.GetClaimsAsync(role))
                .Where(c => c.Type == AccountRole.PermissionClaimType)
                .Select(c => c.Value)
                .ToHashSet();
            if (!current.SetEquals(parsed.Select(p => p.ToString())))
            {
                return Error.Validation("The Admin role's permission set cannot be edited.");
            }
        }

        role.Rename(cmd.Name, cmd.NameAr);
        var updateResult = await roles.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            return Error.Validation(string.Join(' ', updateResult.Errors.Select(e => e.Description)));
        }

        if (!role.IsSystemOwned)
        {
            var existing = await roles.GetClaimsAsync(role);
            foreach (var claim in existing.Where(c => c.Type == AccountRole.PermissionClaimType))
                await roles.RemoveClaimAsync(role, claim);
            foreach (var permission in parsed)
                await roles.AddClaimAsync(role, new System.Security.Claims.Claim(AccountRole.PermissionClaimType, permission.ToString()));
        }

        return await role.ToDtoAsync(roles);
    }
}

internal static class UpdateRoleEndpoint
{
    public static void MapUpdateRoleEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateRoleRequest body, UpdateRoleHandler h, CancellationToken ct) =>
            (await h.Handle(new UpdateRoleCommand(id, body.Name, body.NameAr, body.Permissions), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
