using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record ListRolesQuery(bool IncludeClosed = false);

internal sealed class ListRolesHandler(CoreDbContext db, RoleManager<AccountRole> roles)
    : IQueryHandler<ListRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(ListRolesQuery query, CancellationToken ct)
    {
        var q = db.AccountRoles.AsQueryable();
        if (!query.IncludeClosed) q = q.Where(r => r.Status == RoleStatus.Active);

        var rows = await q.OrderBy(r => r.Name).ToListAsync(ct);
        var dtos = new List<RoleDto>(rows.Count);
        foreach (var role in rows) dtos.Add(await role.ToDtoAsync(roles));
        return Result<IReadOnlyList<RoleDto>>.Success(dtos);
    }
}

internal static class ListRolesEndpoint
{
    public static void MapListRolesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListRolesHandler h, CancellationToken ct, bool includeClosed = false) =>
            (await h.Handle(new ListRolesQuery(includeClosed), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
