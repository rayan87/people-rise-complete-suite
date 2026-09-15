using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Grades;

public sealed record ListGradesQuery(bool IncludeClosed = false);

// Closed grades are excluded from pickers by default; historical views pass IncludeClosed
// (Core Spec §3.4).
internal sealed class ListGradesHandler(CoreDbContext db)
    : IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>>
{
    public async Task<Result<IReadOnlyList<GradeDto>>> Handle(ListGradesQuery query, CancellationToken ct)
    {
        var q = db.Grades.AsQueryable();
        if (!query.IncludeClosed) q = q.Where(g => g.Status == GradeStatus.Active);

        var rows = await q.OrderBy(g => g.Rank)
            .Select(g => new GradeDto(g.Id, g.Code, g.NameEn, g.NameAr, g.Rank, g.LevelId, g.Level!.Code, g.Status.ToString()))
            .ToListAsync(ct);
        return Result<IReadOnlyList<GradeDto>>.Success(rows);
    }
}

internal static class ListGradesEndpoint
{
    public static void MapListGradesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListGradesHandler h, CancellationToken ct, bool includeClosed = false) =>
            (await h.Handle(new ListGradesQuery(includeClosed), ct)).ToHttp());
    }
}
