using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record CloseAccountCommand(Guid Id);

// Core Spec §11: "a separated employee's account is closed... two events on two records, and
// neither cascades into the other" - closing is always an explicit call, never triggered by
// Employee.Separate(). Closed, never deleted.
internal sealed class CloseAccountHandler(UserManager<Account> users)
    : ICommandHandler<CloseAccountCommand, Result<AccountDto>>
{
    public async Task<Result<AccountDto>> Handle(CloseAccountCommand cmd, CancellationToken ct)
    {
        var account = await users.FindByIdAsync(cmd.Id.ToString());
        if (account is null) return Error.NotFound("Account not found.");

        account.Close();
        var result = await users.UpdateAsync(account);
        return result.Succeeded
            ? account.ToDto()
            : Error.Validation(string.Join(' ', result.Errors.Select(e => e.Description)));
    }
}

internal static class CloseAccountEndpoint
{
    public static void MapCloseAccountEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/close", async (Guid id, CloseAccountHandler h, CancellationToken ct) =>
            (await h.Handle(new CloseAccountCommand(id), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
