using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Grades;
using PeopleRise.Core.Application.Jobs;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

public sealed record ListEvaluationsQuery();

internal sealed class ListEvaluationsHandler(
    JobRewardDbContext db,
    IQueryHandler<ListJobsQuery, Result<IReadOnlyList<JobDto>>> listJobs,
    IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>> listGrades)
    : IQueryHandler<ListEvaluationsQuery, Result<IReadOnlyList<EvaluationListItemDto>>>
{
    public async Task<Result<IReadOnlyList<EvaluationListItemDto>>> Handle(ListEvaluationsQuery query, CancellationToken ct)
    {
        var evaluations = await db.Evaluations
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        // Job and Grade live in PeopleRise.Core - fetch both lists once and zip in C#, rather than
        // one row at a time, since they can no longer be joined into the Evaluation query itself.
        var jobsResult = await listJobs.Handle(new ListJobsQuery(), ct);
        if (jobsResult.IsFailure) return jobsResult.Error!;
        var jobsById = jobsResult.Value.ToDictionary(j => j.Id);

        var gradesResult = await listGrades.Handle(new ListGradesQuery(), ct);
        if (gradesResult.IsFailure) return gradesResult.Error!;
        var gradesById = gradesResult.Value.ToDictionary(g => g.Id);

        var rows = evaluations.Select(e =>
        {
            jobsById.TryGetValue(e.JobId, out var job);
            GradeDto? grade = e.RecommendedGradeId is { } gradeId ? gradesById.GetValueOrDefault(gradeId) : null;
            return new EvaluationListItemDto(
                e.Id, e.JobId, job?.Code ?? "", job?.TitleEn ?? "", job?.TitleAr,
                e.MethodologyVersionId, e.Status.ToString(), e.TotalScore,
                e.RecommendedGradeId, grade?.Code, e.CreatedAt);
        }).ToList();

        return Result<IReadOnlyList<EvaluationListItemDto>>.Success(rows);
    }
}

internal static class ListEvaluationsEndpoint
{
    public static void MapListEvaluationsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListEvaluationsHandler h, CancellationToken ct) =>
            (await h.Handle(new ListEvaluationsQuery(), ct)).ToHttp());
    }
}
