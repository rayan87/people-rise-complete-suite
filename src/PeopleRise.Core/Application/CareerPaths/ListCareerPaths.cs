using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.CareerPaths;

public sealed record ListCareerPathsQuery(Guid? JobFamilyId = null);

internal sealed class ListCareerPathsHandler(CoreDbContext db)
    : IQueryHandler<ListCareerPathsQuery, Result<IReadOnlyList<CareerPathDto>>>
{
    public async Task<Result<IReadOnlyList<CareerPathDto>>> Handle(ListCareerPathsQuery query, CancellationToken ct)
    {
        var q = db.CareerPaths.AsQueryable();
        if (query.JobFamilyId is { } familyId) q = q.Where(p => p.JobFamilyId == familyId);

        var ids = await q.OrderBy(p => p.NameEn).Select(p => p.Id).ToListAsync(ct);
        var rows = new List<CareerPathDto>(ids.Count);
        foreach (var id in ids)
        {
            var dto = await CareerPathProjections.ByIdAsync(db, id, ct);
            if (dto is not null) rows.Add(dto);
        }
        return Result<IReadOnlyList<CareerPathDto>>.Success(rows);
    }
}

internal static class ListCareerPathsEndpoint
{
    public static void MapListCareerPathsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListCareerPathsHandler h, CancellationToken ct, Guid? jobFamilyId = null) =>
            (await h.Handle(new ListCareerPathsQuery(jobFamilyId), ct)).ToHttp());
    }
}
