using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Grades;

public sealed record ListGradesQuery();

internal sealed class ListGradesHandler(JobRewardDbContext db)
    : IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>>
{
    public async Task<Result<IReadOnlyList<GradeDto>>> Handle(ListGradesQuery query, CancellationToken ct)
    {
        var rows = await db.Grades.OrderBy(g => g.Rank)
            .Select(g => new GradeDto(g.Id, g.Code, g.NameEn, g.NameAr, g.Rank, g.LevelId, g.Level!.Code))
            .ToListAsync(ct);
        return Result<IReadOnlyList<GradeDto>>.Success(rows);
    }
}
