using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Jobs;

public sealed record AssignJobGradeCommand(Guid JobId, Guid GradeId);

internal sealed class AssignJobGradeHandler(JobRewardDbContext db)
    : ICommandHandler<AssignJobGradeCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(AssignJobGradeCommand cmd, CancellationToken ct)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == cmd.JobId, ct);
        if (job is null) return Error.NotFound("Job not found.");
        var gradeExists = await db.Grades.AnyAsync(g => g.Id == cmd.GradeId, ct);
        if (!gradeExists) return Error.NotFound("Grade not found.");

        try { job.AssignGrade(cmd.GradeId, GradeSource.Assigned); }
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
