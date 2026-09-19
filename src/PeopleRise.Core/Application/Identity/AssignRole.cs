using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record AssignRoleCommand(Guid AccountId, Guid RoleId);

internal sealed class AssignRoleHandler(UserManager<Account> users, RoleManager<AccountRole> roles)
    : ICommandHandler<AssignRoleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AssignRoleCommand cmd, CancellationToken ct)
    {
        var account = await users.FindByIdAsync(cmd.AccountId.ToString());
        if (account is null) return Error.NotFound("Account not found.");

        var role = await roles.FindByIdAsync(cmd.RoleId.ToString());
        if (role is null) return Error.NotFound("Role not found.");

        if (await users.IsInRoleAsync(account, role.Name!))
        {
            return Error.Conflict("This account already has this role.");
        }

        var result = await users.AddToRoleAsync(account, role.Name!);
        return result.Succeeded
            ? Result<bool>.Success(true)
            : Error.Validation(string.Join(' ', result.Errors.Select(e => e.Description)));
    }
}

internal static class AssignRoleEndpoint
{
    public static void MapAssignRoleEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/assignments", async (AssignRoleRequest body, AssignRoleHandler h, CancellationToken ct) =>
            (await h.Handle(new AssignRoleCommand(body.AccountId, body.RoleId), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
