using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Grades;
using PeopleRise.Core.Application.Jobs;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

public sealed record ApproveEvaluationCommand(Guid EvaluationId);

internal sealed class ApproveEvaluationHandler(
    JobRewardDbContext db,
    IQueryHandler<GetJobQuery, Result<JobDto>> getJob,
    IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>> listGrades,
    ICommandHandler<AssignJobGradeCommand, Result<JobDto>> assignJobGrade)
    : ICommandHandler<ApproveEvaluationCommand, Result<EvaluationResultDto>>
{
    public async Task<Result<EvaluationResultDto>> Handle(ApproveEvaluationCommand cmd, CancellationToken ct)
    {
        var evaluation = await db.Evaluations.FirstOrDefaultAsync(e => e.Id == cmd.EvaluationId, ct);

        if (evaluation is null)
        {
            return Error.NotFound("Evaluation not found.");
        }

        try
        {
            evaluation.Approve();
        }
        catch (DomainStateException e)
        {
            return Error.Conflict(e.Message);
        }

        // Pipeline outcome: an approved evaluation stamps the recommended grade onto the job (the
        // job itself lives in PeopleRise.Core - assigned via its public command, not a local Include).
        if (evaluation.RecommendedGradeId is { } gradeId)
        {
            // immutable rule: never edit a prior evaluation; supersede it (kept for history)
            var priorApproved = await db.Evaluations
                .Where(e => e.JobId == evaluation.JobId && e.Status == EvaluationStatus.Approved && e.Id != evaluation.Id)
                .ToListAsync(ct);
            foreach (var p in priorApproved) p.Supersede();

            var assignResult = await assignJobGrade.Handle(
                new AssignJobGradeCommand(evaluation.JobId, gradeId, GradeAssignmentSource.Evaluated), ct);
            if (assignResult.IsFailure) return assignResult.Error!;
        }

        await db.SaveChangesAsync(ct);

        return (await EvaluationProjections.BuildAsync(db, getJob, listGrades, evaluation.Id, ct))!;
    }
}

internal static class ApproveEvaluationEndpoint
{
    public static void MapApproveEvaluationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/approve", async (Guid id, ApproveEvaluationHandler h, CancellationToken ct) =>
            (await h.Handle(new ApproveEvaluationCommand(id), ct)).ToHttp());
    }
}

