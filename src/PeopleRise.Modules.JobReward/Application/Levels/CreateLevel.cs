using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Levels;

public sealed record CreateLevelCommand(string Code, string NameEn, string? NameAr, int Rank, bool InEvalScope = true);

internal sealed class CreateLevelHandler(JobRewardDbContext db)
    : ICommandHandler<CreateLevelCommand, Result<LevelDto>>
{
    public async Task<Result<LevelDto>> Handle(CreateLevelCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var level = Level.Create(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.Rank, cmd.InEvalScope);

        db.Levels.Add(level);
        await db.SaveChangesAsync(ct);
        return new LevelDto(level.Id, level.Code, level.NameEn, level.NameAr, level.Rank, level.InEvalScope);
    }
}

internal static class CreatLevelEndpoint
{
    public static void MapCreateLevelEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateLevelCommand cmd, CreateLevelHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());
    }
}
