using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Levels;

public sealed record DeleteLevelCommand(Guid Id);

internal sealed class DeleteLevelHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteLevelCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteLevelCommand cmd, CancellationToken ct)
    {
        var level = await db.Levels.FindAsync(cmd.Id, ct);

        if (level is null)
        {
            return Error.NotFound("Level not found.");
        }

        var gradeCount = await db.Grades.CountAsync(g => g.LevelId == cmd.Id, ct);

        if (gradeCount > 0)
        {
            return Error.Conflict($"Level is in use by {gradeCount} grade(s) — reassign them before deleting.");
        }

        db.Levels.Remove(level);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteLevelEndpoint
{
    public static void MapDeleteLevelEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteLevelHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteLevelCommand(id), ct)).ToHttp());
    }
}
