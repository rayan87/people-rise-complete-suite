using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Jobs;

// Public mirror of the internal Domain.GradeSource vocabulary (Core Spec §3.1/§6: Evaluated ·
// ManuallyAssigned) - the domain enum itself stays internal like every other Core entity/type;
// this is the one place it crosses the module boundary, e.g. Job Evaluation assigning a grade via
// its own Submit/Approve flow (Core Spec §11.2: Job Evaluation may write this fact, with this
// provenance).
public enum GradeAssignmentSource { Evaluated, ManuallyAssigned }

public sealed record AssignJobGradeCommand(Guid JobId, Guid GradeId, GradeAssignmentSource Source = GradeAssignmentSource.ManuallyAssigned);

internal sealed class AssignJobGradeHandler(CoreDbContext db)
    : ICommandHandler<AssignJobGradeCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(AssignJobGradeCommand cmd, CancellationToken ct)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == cmd.JobId, ct);
        if (job is null) return Error.NotFound("Job not found.");
        var gradeExists = await db.Grades.AnyAsync(g => g.Id == cmd.GradeId, ct);
        if (!gradeExists) return Error.NotFound("Grade not found.");

        var source = cmd.Source == GradeAssignmentSource.Evaluated ? GradeSource.Evaluated : GradeSource.ManuallyAssigned;
        try { job.AssignGrade(cmd.GradeId, source); }
        catch (DomainStateException e) { return Error.Conflict(e.Message); }

        await db.SaveChangesAsync(ct);
        return (await new GetJobHandler(db).Handle(new GetJobQuery(job.Id), ct));
    }
}

internal static class AssignJobGradeEndpoint
{
    public static void MapAssignJobGradeEndpoint(this RouteGroupBuilder group) =>
        group.MapPost("/{id:guid}/grade", async (Guid id, AssignGradeRequest body,
                                                 AssignJobGradeHandler h, CancellationToken ct) =>
            (await h.Handle(new AssignJobGradeCommand(id, body.GradeId), ct)).ToHttp());
}

public sealed record AssignGradeRequest(Guid GradeId);
