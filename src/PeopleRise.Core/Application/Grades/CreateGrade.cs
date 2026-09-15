using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Grades;

public sealed record CreateGradeCommand(string Code, string NameEn, string? NameAr, int Rank, Guid LevelId);

internal sealed class CreateGradeHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<CreateGradeCommand, Result<GradeDto>>
{
    public async Task<Result<GradeDto>> Handle(CreateGradeCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        if (!await db.Levels.AnyAsync(l => l.Id == cmd.LevelId, ct))
        {
            return Error.NotFound("Level not found.");
        }

        var grade = Grade.Create(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.Rank, cmd.LevelId);
        db.Grades.Add(grade);
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new GradeCreated(grade.Id, grade.Code), ct);
        return new GradeDto(grade.Id, grade.Code, grade.NameEn, grade.NameAr, grade.Rank, grade.LevelId, null, grade.Status.ToString());
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
