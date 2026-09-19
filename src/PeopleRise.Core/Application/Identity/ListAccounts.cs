using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record ListAccountsQuery(bool IncludeClosed = false);

internal sealed class ListAccountsHandler(CoreDbContext db)
    : IQueryHandler<ListAccountsQuery, Result<IReadOnlyList<AccountDto>>>
{
    public async Task<Result<IReadOnlyList<AccountDto>>> Handle(ListAccountsQuery query, CancellationToken ct)
    {
        var q = db.Accounts.AsQueryable();
        if (!query.IncludeClosed) q = q.Where(a => a.Status == AccountStatus.Active);

        var rows = await q.OrderBy(a => a.Email).ToListAsync(ct);
        return Result<IReadOnlyList<AccountDto>>.Success(rows.Select(a => a.ToDto()).ToList());
    }
}

internal static class ListAccountsEndpoint
{
    public static void MapListAccountsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListAccountsHandler h, CancellationToken ct, bool includeClosed = false) =>
            (await h.Handle(new ListAccountsQuery(includeClosed), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
