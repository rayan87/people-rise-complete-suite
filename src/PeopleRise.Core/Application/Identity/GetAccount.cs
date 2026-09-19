using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record GetAccountQuery(Guid Id);

internal sealed class GetAccountHandler(UserManager<Account> users)
    : IQueryHandler<GetAccountQuery, Result<AccountDto>>
{
    public async Task<Result<AccountDto>> Handle(GetAccountQuery query, CancellationToken ct)
    {
        var account = await users.FindByIdAsync(query.Id.ToString());
        return account is null ? Error.NotFound("Account not found.") : account.ToDto();
    }
}

internal static class GetAccountEndpoint
{
    public static void MapGetAccountEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, GetAccountHandler h, CancellationToken ct) =>
            (await h.Handle(new GetAccountQuery(id), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
