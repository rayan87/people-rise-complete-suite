using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Levels;

public sealed record ListLevelsQuery();

internal sealed class ListLevelsHandler(JobRewardDbContext db)
    : IQueryHandler<ListLevelsQuery, Result<IReadOnlyList<LevelDto>>>
{
    public async Task<Result<IReadOnlyList<LevelDto>>> Handle(ListLevelsQuery query, CancellationToken ct)
    {
        var rows = await db.Levels.OrderBy(l => l.Rank)
            .Select(l => new LevelDto(l.Id, l.Code, l.NameEn, l.NameAr, l.Rank, l.InEvalScope))
            .ToListAsync(ct);
        return Result<IReadOnlyList<LevelDto>>.Success(rows);
    }
}
