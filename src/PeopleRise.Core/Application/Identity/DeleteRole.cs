using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record DeleteRoleCommand(Guid Id);

// Closed, never deleted, once assigned to at least one account (Core Spec §3.4). The seeded Admin
// role can never be deleted at all (§11.2), close or otherwise.
internal sealed class DeleteRoleHandler(RoleManager<AccountRole> roles, UserManager<Account> users)
    : ICommandHandler<DeleteRoleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteRoleCommand cmd, CancellationToken ct)
    {
        var role = await roles.FindByIdAsync(cmd.Id.ToString());
        if (role is null) return Error.NotFound("Role not found.");
        if (role.IsSystemOwned) return Error.Validation("The Admin role cannot be deleted.");

        var members = await users.GetUsersInRoleAsync(role.Name!);
        if (members.Count > 0)
        {
            role.Close();
            var closeResult = await roles.UpdateAsync(role);
            return closeResult.Succeeded
                ? Result<bool>.Success(true)
                : Error.Validation(string.Join(' ', closeResult.Errors.Select(e => e.Description)));
        }

        var deleteResult = await roles.DeleteAsync(role);
        return deleteResult.Succeeded
            ? Result<bool>.Success(true)
            : Error.Validation(string.Join(' ', deleteResult.Errors.Select(e => e.Description)));
    }
}

internal static class DeleteRoleEndpoint
{
    public static void MapDeleteRoleEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteRoleHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteRoleCommand(id), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
