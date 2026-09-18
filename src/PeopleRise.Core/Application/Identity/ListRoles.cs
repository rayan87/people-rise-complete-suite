using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Permissions;

public sealed record ListRolesQuery(bool IncludeClosed = false);

internal sealed class ListRolesHandler(CoreDbContext db)
    : IQueryHandler<ListRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(ListRolesQuery query, CancellationToken ct)
    {
        var q = db.Roles.AsQueryable();
        if (!query.IncludeClosed) q = q.Where(r => r.Status == RoleStatus.Active);

        var rows = await q.OrderBy(r => r.NameEn).ToListAsync(ct);
        return Result<IReadOnlyList<RoleDto>>.Success(
            rows.Select(r => new RoleDto(r.Id, r.NameEn, r.NameAr, r.Permissions.Select(p => p.ToString()).ToList(), r.Status.ToString())).ToList());
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
