using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Grades;

public sealed record CreateGradeCommand(string Code, string NameEn, string? NameAr, int Rank, Guid? LevelId = null);

internal sealed class CreateGradeHandler(JobRewardDbContext db)
    : ICommandHandler<CreateGradeCommand, Result<GradeDto>>
{
    public async Task<Result<GradeDto>> Handle(CreateGradeCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        if (cmd.LevelId is { } lid && !await db.Levels.AnyAsync(l => l.Id == lid, ct))
        {
            return Error.NotFound("Level not found.");
        }
            
        var grade = Grade.Create(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.Rank, cmd.LevelId);
        db.Grades.Add(grade);
        await db.SaveChangesAsync(ct);
        return new GradeDto(grade.Id, grade.Code, grade.NameEn, grade.NameAr, grade.Rank, grade.LevelId, null);
    }
}

internal static class CreatGradeEndpoint
{
    public static void MapCreateGradeEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateGradeCommand cmd, CreateGradeHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());
    }
}
