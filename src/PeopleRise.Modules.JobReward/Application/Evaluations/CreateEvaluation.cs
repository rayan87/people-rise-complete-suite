using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Jobs;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

public sealed record CreateEvaluationCommand(Guid JobId, Guid MethodologyVersionId, Guid? EvaluatorEmployeeId = null);

internal sealed class CreateEvaluationHandler(
    JobRewardDbContext db, IQueryHandler<GetJobQuery, Result<JobDto>> getJob)
    : ICommandHandler<CreateEvaluationCommand, Result<EvaluationCreatedDto>>
{
    public async Task<Result<EvaluationCreatedDto>> Handle(CreateEvaluationCommand cmd, CancellationToken ct)
    {
        // Job (and the evaluator, a core Employee) live in PeopleRise.Core.
        var jobResult = await getJob.Handle(new GetJobQuery(cmd.JobId), ct);
        if (jobResult.IsFailure)
        {
            return Error.NotFound("Job not found.");
        }

        var version = await db.MethodologyVersions
            .FirstOrDefaultAsync(v => v.Id == cmd.MethodologyVersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        if (version.Status != MethodologyVersionStatus.Active)
        {
            return Error.Validation($"Methodology version is {version.Status}; evaluations can only pin an Active version.");
        }

        // NOTE: pre-extraction this also verified the evaluator employee exists (Employee is now a
        // core entity with no wired lookup-by-id contract yet - EvaluatorEmployeeId is accepted
        // as-is; add a Core query here once one exists).

        var evaluation = Evaluation.CreateDraft(cmd.JobId, cmd.MethodologyVersionId, cmd.EvaluatorEmployeeId);
        db.Evaluations.Add(evaluation);
        await db.SaveChangesAsync(ct);
        return new EvaluationCreatedDto(evaluation.Id, 
            evaluation.JobId,
            evaluation.MethodologyVersionId, 
            evaluation.Status.ToString());
    }
}

internal static class CreateEvaluationEndpoint
{
    public static void MapCreateEvaluationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateEvaluationCommand cmd, CreateEvaluationHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());
    }
}

