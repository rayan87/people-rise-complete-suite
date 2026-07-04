using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Jobs;

public sealed record DeleteJobCommand(Guid Id);

internal sealed class DeleteJobHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteJobCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteJobCommand cmd, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync(cmd.Id, ct);

        if (job is null)
        {
            return Error.NotFound("Job not found.");
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
