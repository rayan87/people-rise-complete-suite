using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.CareerPaths;

public sealed record CreateCareerPathCommand(Guid JobFamilyId, string NameEn, string? NameAr, IReadOnlyList<Guid> GradeIdsInOrder);

internal sealed class CreateCareerPathHandler(CoreDbContext db)
    : ICommandHandler<CreateCareerPathCommand, Result<CareerPathDto>>
{
    public async Task<Result<CareerPathDto>> Handle(CreateCareerPathCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        if (!await db.JobFamilies.AnyAsync(f => f.Id == cmd.JobFamilyId, ct))
        {
            return Error.NotFound("Job family not found.");
        }

        if (cmd.GradeIdsInOrder.Count != cmd.GradeIdsInOrder.Distinct().Count())
        {
            return Error.Validation("A grade cannot appear twice in the same path.");
        }

        var knownCount = await db.Grades.CountAsync(g => cmd.GradeIdsInOrder.Contains(g.Id), ct);
        if (knownCount != cmd.GradeIdsInOrder.Count)
        {
            return Error.Validation("One or more grades were not found.");
        }

        var path = CareerPath.Create(cmd.JobFamilyId, cmd.NameEn, cmd.NameAr);
        db.CareerPaths.Add(path);
        for (var i = 0; i < cmd.GradeIdsInOrder.Count; i++)
        {
            db.CareerPathSteps.Add(CareerPathStep.Create(path.Id, cmd.GradeIdsInOrder[i], i + 1));
        }
        await db.SaveChangesAsync(ct);

        return (await CareerPathProjections.ByIdAsync(db, path.Id, ct))!;
    }
}

internal static class CreateCareerPathEndpoint
{
    public static void MapCreateCareerPathEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateCareerPathRequest body, CreateCareerPathHandler h, CancellationToken ct) =>
            (await h.Handle(new CreateCareerPathCommand(body.JobFamilyId, body.NameEn, body.NameAr, body.GradeIdsInOrder), ct)).ToHttp());
    }
}
