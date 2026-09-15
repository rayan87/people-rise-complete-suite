using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Jobs;

public sealed record DeleteJobCommand(Guid Id);

// Closed, never deleted, once anything has referenced it (Core Spec §3.4/§5 - a job that's been
// evaluated is the spec's own named example). "Ever referenced" = it has ever held a grade
// assignment or a position, both retained tables, so this check covers full history, not just
// current state.
internal sealed class DeleteJobHandler(CoreDbContext db)
    : ICommandHandler<DeleteJobCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteJobCommand cmd, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync(cmd.Id, ct);

        if (job is null)
        {
            return Error.NotFound("Job not found.");
        }

        var everReferenced = await db.JobGradeAssignments.AnyAsync(a => a.JobId == cmd.Id, ct)
            || await db.JobPositions.AnyAsync(p => p.JobId == cmd.Id, ct);

        if (everReferenced)
        {
            job.Archive();
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }

        db.Jobs.Remove(job);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteJobEndpoint
{
    public static void MapDeleteJobEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteJobHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteJobCommand(id), ct)).ToHttp());
    }
}
