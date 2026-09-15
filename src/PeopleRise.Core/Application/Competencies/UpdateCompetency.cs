using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

public sealed record UpdateCompetencyCommand(Guid Id, string Code, string NameEn, string? NameAr, string? DescriptionEn, string? DescriptionAr);

internal sealed class UpdateCompetencyHandler(CoreDbContext db)
    : ICommandHandler<UpdateCompetencyCommand, Result<CompetencyDto>>
{
    public async Task<Result<CompetencyDto>> Handle(UpdateCompetencyCommand cmd, CancellationToken ct)
    {
        var competency = await db.CompetencyDefinitions.FindAsync([cmd.Id], ct);
        if (competency is null) return Error.NotFound("Competency not found.");
        if (string.IsNullOrWhiteSpace(cmd.NameEn)) return Error.Validation("English name is required.");

        competency.Update(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.DescriptionEn, cmd.DescriptionAr);
        await db.SaveChangesAsync(ct);
        return new CompetencyDto(competency.Id, competency.Code, competency.NameEn, competency.NameAr,
            competency.DescriptionEn, competency.DescriptionAr, competency.Provenance.ToString(), competency.Status.ToString());
    }
}

internal static class UpdateCompetencyEndpoint
{
    public static void MapUpdateCompetencyEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateCompetencyRequest body, UpdateCompetencyHandler h, CancellationToken ct) =>
            (await h.Handle(new UpdateCompetencyCommand(id, body.Code, body.NameEn, body.NameAr, body.DescriptionEn, body.DescriptionAr), ct)).ToHttp());
    }
}
