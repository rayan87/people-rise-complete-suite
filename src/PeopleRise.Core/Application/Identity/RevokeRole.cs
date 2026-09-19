using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record RevokeRoleCommand(Guid AccountId, Guid RoleId);

// A grant, not an effective-dated or provenance-bearing record - revoking simply removes the
// Identity user-role row (nothing else ever references it).
internal sealed class RevokeRoleHandler(UserManager<Account> users, RoleManager<AccountRole> roles)
    : ICommandHandler<RevokeRoleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RevokeRoleCommand cmd, CancellationToken ct)
    {
        var account = await users.FindByIdAsync(cmd.AccountId.ToString());
        if (account is null) return Error.NotFound("Account not found.");

        var role = await roles.FindByIdAsync(cmd.RoleId.ToString());
        if (role is null) return Error.NotFound("Role not found.");

        var result = await users.RemoveFromRoleAsync(account, role.Name!);
        return result.Succeeded
            ? Result<bool>.Success(true)
            : Error.Validation(string.Join(' ', result.Errors.Select(e => e.Description)));
    }
}

internal static class RevokeRoleEndpoint
{
    public static void MapRevokeRoleEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/assignments/{accountId:guid}/{roleId:guid}", async (Guid accountId, Guid roleId, RevokeRoleHandler h, CancellationToken ct) =>
            (await h.Handle(new RevokeRoleCommand(accountId, roleId), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
