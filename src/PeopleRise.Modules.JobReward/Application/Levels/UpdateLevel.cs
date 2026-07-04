using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Levels;

public sealed record UpdateLevelCommand(Guid Id, string Code, string NameEn, string? NameAr, int Rank, bool InEvalScope);

internal sealed class UpdateLevelHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateLevelCommand, Result<LevelDto>>
{
    public async Task<Result<LevelDto>> Handle(UpdateLevelCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var level = await db.Levels.FindAsync(cmd.Id, ct);

        if (level is null)
        {
            return Error.NotFound("Level not found.");
        }

        level.Update(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.Rank, cmd.InEvalScope);
        await db.SaveChangesAsync(ct);
        return new LevelDto(level.Id, level.Code, level.NameEn, level.NameAr, level.Rank, level.InEvalScope);
    }
}

internal static class UpdateLevelEndpoint
{
    public static void MapUpdateLevelEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateLevelCommand cmd, UpdateLevelHandler h, CancellationToken ct) =>
            (await h.Handle(cmd with { Id = id }, ct)).ToHttp());
    }
}