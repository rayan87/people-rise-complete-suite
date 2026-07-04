using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

public sealed record ApproveEvaluationCommand(Guid EvaluationId);

internal sealed class ApproveEvaluationHandler(JobRewardDbContext db)
    : ICommandHandler<ApproveEvaluationCommand, Result<EvaluationResultDto>>
{
    public async Task<Result<EvaluationResultDto>> Handle(ApproveEvaluationCommand cmd, CancellationToken ct)
    {
        var evaluation = await db.Evaluations.Include(e => e.Job)
            .FirstOrDefaultAsync(e => e.Id == cmd.EvaluationId, ct);

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

        // Pipeline outcome: an approved evaluation stamps the recommended grade onto the job.
        if (evaluation.RecommendedGradeId is { } gradeId 
            && evaluation.Job is { } job)
        {
            job.AssignGrade(gradeId);
        }
        
        await db.SaveChangesAsync(ct);

        return (await EvaluationProjections.BuildAsync(db, evaluation.Id, ct))!;
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

