using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

public sealed record ListEvaluationsQuery();

internal sealed class ListEvaluationsHandler(JobRewardDbContext db)
    : IQueryHandler<ListEvaluationsQuery, Result<IReadOnlyList<EvaluationListItemDto>>>
{
    public async Task<Result<IReadOnlyList<EvaluationListItemDto>>> Handle(ListEvaluationsQuery query, CancellationToken ct)
    {
        var rows = await db.Evaluations
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EvaluationListItemDto(
                e.Id, e.JobId, e.Job!.Code, e.Job.TitleEn, e.Job.TitleAr,
                e.MethodologyVersionId, e.Status.ToString(), e.TotalScore,
                e.RecommendedGradeId, e.RecommendedGrade!.Code, e.CreatedAt))
            .ToListAsync(ct);
        return Result<IReadOnlyList<EvaluationListItemDto>>.Success(rows);
    }
}
