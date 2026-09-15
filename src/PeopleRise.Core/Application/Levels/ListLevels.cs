using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Levels;

public sealed record ListLevelsQuery(bool IncludeClosed = false);

internal sealed class ListLevelsHandler(CoreDbContext db)
    : IQueryHandler<ListLevelsQuery, Result<IReadOnlyList<LevelDto>>>
{
    public async Task<Result<IReadOnlyList<LevelDto>>> Handle(ListLevelsQuery query, CancellationToken ct)
    {
        var q = db.Levels.AsQueryable();
        if (!query.IncludeClosed) q = q.Where(l => l.Status == LevelStatus.Active);

        var rows = await q.OrderBy(l => l.Rank)
            .Select(l => new LevelDto(l.Id, l.Code, l.NameEn, l.NameAr, l.Rank, l.Status.ToString()))
            .ToListAsync(ct);
        return Result<IReadOnlyList<LevelDto>>.Success(rows);
    }
}

internal static class ListLevelsEndpoint
{
    public static void MapListLevelsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListLevelsHandler h, CancellationToken ct, bool includeClosed = false) =>
            (await h.Handle(new ListLevelsQuery(includeClosed), ct)).ToHttp());
    }
}
