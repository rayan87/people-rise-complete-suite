using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

// Core UI writes TenantAuthored (Core Spec §3.1/§10/§11.2) - Seeded comes from the provisioning
// seed, Granted from a future control-plane library (not built, mirrors Methodology's deferred
// grant mechanism).
public sealed record CreateCompetencyCommand(string Code, string NameEn, string? NameAr, string? DescriptionEn, string? DescriptionAr);

internal sealed class CreateCompetencyHandler(CoreDbContext db)
    : ICommandHandler<CreateCompetencyCommand, Result<CompetencyDto>>
{
    public async Task<Result<CompetencyDto>> Handle(CreateCompetencyCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var competency = CompetencyDefinition.Create(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.DescriptionEn, cmd.DescriptionAr, CompetencyProvenance.TenantAuthored);
        db.CompetencyDefinitions.Add(competency);
        await db.SaveChangesAsync(ct);
        return new CompetencyDto(competency.Id, competency.Code, competency.NameEn, competency.NameAr,
            competency.DescriptionEn, competency.DescriptionAr, competency.Provenance.ToString(), competency.Status.ToString());
    }
}

internal static class CreateCompetencyEndpoint
{
    public static void MapCreateCompetencyEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateCompetencyRequest body, CreateCompetencyHandler h, CancellationToken ct) =>
            (await h.Handle(new CreateCompetencyCommand(body.Code, body.NameEn, body.NameAr, body.DescriptionEn, body.DescriptionAr), ct)).ToHttp());
    }
}
