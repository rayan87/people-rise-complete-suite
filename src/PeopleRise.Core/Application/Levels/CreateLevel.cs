using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Levels;

public sealed record CreateLevelCommand(string Code, string NameEn, string? NameAr, int Rank);

internal sealed class CreateLevelHandler(CoreDbContext db)
    : ICommandHandler<CreateLevelCommand, Result<LevelDto>>
{
    public async Task<Result<LevelDto>> Handle(CreateLevelCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var level = Level.Create(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.Rank);

        db.Levels.Add(level);
        await db.SaveChangesAsync(ct);
        return new LevelDto(level.Id, level.Code, level.NameEn, level.NameAr, level.Rank);
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
