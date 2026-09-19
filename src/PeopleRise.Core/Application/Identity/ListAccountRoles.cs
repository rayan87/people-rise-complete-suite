using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record ListAccountRolesQuery(Guid AccountId);

internal sealed class ListAccountRolesHandler(UserManager<Account> users)
    : IQueryHandler<ListAccountRolesQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(ListAccountRolesQuery query, CancellationToken ct)
    {
        var account = await users.FindByIdAsync(query.AccountId.ToString());
        if (account is null) return Error.NotFound("Account not found.");

        var roleNames = await users.GetRolesAsync(account);
        return Result<IReadOnlyList<string>>.Success(roleNames.ToList());
    }
}

internal static class ListAccountRolesEndpoint
{
    public static void MapListAccountRolesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/roles", async (Guid id, ListAccountRolesHandler h, CancellationToken ct) =>
            (await h.Handle(new ListAccountRolesQuery(id), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
