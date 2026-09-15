using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Levels;

public sealed record DeleteLevelCommand(Guid Id);

// Closed, never deleted, once anything has referenced it (Core Spec §3.4). Grades are never
// hard-deleted once referenced either, so checking current rows already covers full history.
internal sealed class DeleteLevelHandler(CoreDbContext db)
    : ICommandHandler<DeleteLevelCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteLevelCommand cmd, CancellationToken ct)
    {
        var level = await db.Levels.FindAsync(cmd.Id, ct);

        if (level is null)
        {
            return Error.NotFound("Level not found.");
        }

        var everReferenced = await db.Grades.AnyAsync(g => g.LevelId == cmd.Id, ct);

        if (everReferenced)
        {
            level.Close();
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
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
