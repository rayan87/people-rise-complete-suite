using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.CareerPaths;

// Replaces the path's full step sequence, same "replace-all" shape SetTemplateItems uses for
// competency templates.
public sealed record UpdateCareerPathCommand(Guid Id, string NameEn, string? NameAr, IReadOnlyList<Guid> GradeIdsInOrder);

internal sealed class UpdateCareerPathHandler(CoreDbContext db)
    : ICommandHandler<UpdateCareerPathCommand, Result<CareerPathDto>>
{
    public async Task<Result<CareerPathDto>> Handle(UpdateCareerPathCommand cmd, CancellationToken ct)
    {
        var path = await db.CareerPaths.FirstOrDefaultAsync(p => p.Id == cmd.Id, ct);
        if (path is null) return Error.NotFound("Career path not found.");
        if (string.IsNullOrWhiteSpace(cmd.NameEn)) return Error.Validation("English name is required.");

        if (cmd.GradeIdsInOrder.Count != cmd.GradeIdsInOrder.Distinct().Count())
        {
            return Error.Validation("A grade cannot appear twice in the same path.");
        }

        var knownCount = await db.Grades.CountAsync(g => cmd.GradeIdsInOrder.Contains(g.Id), ct);
        if (knownCount != cmd.GradeIdsInOrder.Count)
        {
            return Error.Validation("One or more grades were not found.");
        }

        path.Update(cmd.NameEn, cmd.NameAr);

        var existing = await db.CareerPathSteps.Where(s => s.CareerPathId == cmd.Id).ToListAsync(ct);
        db.CareerPathSteps.RemoveRange(existing);
        for (var i = 0; i < cmd.GradeIdsInOrder.Count; i++)
        {
            db.CareerPathSteps.Add(CareerPathStep.Create(cmd.Id, cmd.GradeIdsInOrder[i], i + 1));
        }
        await db.SaveChangesAsync(ct);

        return (await CareerPathProjections.ByIdAsync(db, cmd.Id, ct))!;
    }
}

internal static class UpdateCareerPathEndpoint
{
    public static void MapUpdateCareerPathEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateCareerPathRequest body, UpdateCareerPathHandler h, CancellationToken ct) =>
            (await h.Handle(new UpdateCareerPathCommand(id, body.NameEn, body.NameAr, body.GradeIdsInOrder), ct)).ToHttp());
    }
}
