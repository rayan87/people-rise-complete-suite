using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

public sealed record CreateEvaluationCommand(Guid JobId, Guid MethodologyVersionId, Guid? EvaluatorEmployeeId = null);

internal sealed class CreateEvaluationHandler(JobRewardDbContext db)
    : ICommandHandler<CreateEvaluationCommand, Result<EvaluationCreatedDto>>
{
    public async Task<Result<EvaluationCreatedDto>> Handle(CreateEvaluationCommand cmd, CancellationToken ct)
    {
        var job = await db.Jobs.Include(j => j.Level).FirstOrDefaultAsync(j => j.Id == cmd.JobId, ct);
        if (job is null) return Error.NotFound("Job not found.");
        if (job.Level is { InEvalScope: false })
            return Error.Validation($"Job's level '{job.Level.Code}' is out of evaluation scope (e.g. C-level).");

        var version = await db.MethodologyVersions.FirstOrDefaultAsync(v => v.Id == cmd.MethodologyVersionId, ct);
        if (version is null) return Error.NotFound("Methodology version not found.");
        if (version.Status != MethodologyVersionStatus.Active)
            return Error.Validation($"Methodology version is {version.Status}; evaluations can only pin an Active version.");

        if (cmd.EvaluatorEmployeeId is { } empId && !await db.Employees.AnyAsync(e => e.Id == empId, ct))
            return Error.NotFound("Evaluator employee not found.");

        var eval = Evaluation.CreateDraft(cmd.JobId, cmd.MethodologyVersionId, cmd.EvaluatorEmployeeId);
        db.Evaluations.Add(eval);
        await db.SaveChangesAsync(ct);
        return new EvaluationCreatedDto(eval.Id, eval.JobId, eval.MethodologyVersionId, eval.Status.ToString());
    }
}
