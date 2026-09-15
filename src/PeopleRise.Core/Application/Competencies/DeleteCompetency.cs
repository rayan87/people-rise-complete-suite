using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

public sealed record DeleteCompetencyCommand(Guid Id);

// Seeded competencies are fully editable/deletable (Core Spec §10, unlike ISIC) - but closed, never
// deleted, once referenced by a template item, an override, a held profile, or a certification
// (Core Spec §3.4).
internal sealed class DeleteCompetencyHandler(CoreDbContext db)
    : ICommandHandler<DeleteCompetencyCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteCompetencyCommand cmd, CancellationToken ct)
    {
        var competency = await db.CompetencyDefinitions.FindAsync(cmd.Id, ct);
        if (competency is null) return Error.NotFound("Competency not found.");

        var everReferenced = await db.CompetencyTemplateItems.AnyAsync(i => i.CompetencyId == cmd.Id, ct)
            || await db.RequiredCompetencyOverrides.AnyAsync(o => o.CompetencyId == cmd.Id, ct)
            || await db.HeldCompetencyProfiles.AnyAsync(p => p.CompetencyId == cmd.Id, ct)
            || await db.Certifications.AnyAsync(c => c.CompetencyId == cmd.Id, ct);

        if (everReferenced)
        {
            competency.Close();
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }

        db.CompetencyDefinitions.Remove(competency);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteCompetencyEndpoint
{
    public static void MapDeleteCompetencyEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteCompetencyHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteCompetencyCommand(id), ct)).ToHttp());
    }
}
