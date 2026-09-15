using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.OrgUnits;

public sealed record ListOrgUnitsQuery(bool IncludeClosed = false);

// Closed units are excluded from pickers by default; historical views pass IncludeClosed (Core
// Spec §3.4: "closed records are excluded from pickers and retained by every historical query").
internal sealed class ListOrgUnitsHandler(CoreDbContext db)
    : IQueryHandler<ListOrgUnitsQuery, Result<IReadOnlyList<OrgUnitDto>>>
{
    public async Task<Result<IReadOnlyList<OrgUnitDto>>> Handle(ListOrgUnitsQuery query, CancellationToken ct)
    {
        var q = db.OrgUnits.AsQueryable();
        if (!query.IncludeClosed) q = q.Where(u => u.Status == OrgUnitStatus.Active);

        var rows = await q.OrderBy(u => u.Code)
            .Select(u => new OrgUnitDto(u.Id, u.Code, u.NameEn, u.NameAr, u.ParentId,
                u.LocationId, u.LocationId == null ? null : u.Location!.Code, u.Status.ToString()))
            .ToListAsync(ct);
        return Result<IReadOnlyList<OrgUnitDto>>.Success(rows);
    }
}

internal static class ListOrgUnitsEndpoint
{
    public static void MapListOrgUnitsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListOrgUnitsHandler h, CancellationToken ct, bool includeClosed = false) =>
            (await h.Handle(new ListOrgUnitsQuery(includeClosed), ct)).ToHttp());
    }
}
