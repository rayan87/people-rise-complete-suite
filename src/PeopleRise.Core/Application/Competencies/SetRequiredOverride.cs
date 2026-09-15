using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

// RequiredLevel null explicitly EXCLUDES a template-inherited competency from this job (a row that
// exists but overrides to "not required"), distinct from never having an override at all (plain
// template inheritance) - see RemoveRequiredOverride for reverting to inheritance.
public sealed record SetRequiredOverrideCommand(Guid JobId, Guid CompetencyId, int? RequiredLevel);

internal sealed class SetRequiredOverrideHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<SetRequiredOverrideCommand, Result<RequiredProfileDto>>
{
    public async Task<Result<RequiredProfileDto>> Handle(SetRequiredOverrideCommand cmd, CancellationToken ct)
    {
        if (!await db.Jobs.AnyAsync(j => j.Id == cmd.JobId, ct)) return Error.NotFound("Job not found.");
        if (!await db.CompetencyDefinitions.AnyAsync(c => c.Id == cmd.CompetencyId, ct)) return Error.NotFound("Competency not found.");
        if (cmd.RequiredLevel is < 1 or > 5) return Error.Validation("RequiredLevel must be between 1 and 5.");

        var existing = await db.RequiredCompetencyOverrides
            .FirstOrDefaultAsync(o => o.JobId == cmd.JobId && o.CompetencyId == cmd.CompetencyId, ct);

        if (existing is not null)
        {
            existing.SetRequiredLevel(cmd.RequiredLevel);
        }
        else
        {
            var isicVersion = await db.SeedVersions.Where(v => v.Name == "Competency").Select(v => v.Version).FirstOrDefaultAsync(ct);
            db.RequiredCompetencyOverrides.Add(RequiredCompetencyOverride.Create(cmd.JobId, cmd.CompetencyId, cmd.RequiredLevel, isicVersion));
        }

        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new RequiredProfileChanged(cmd.JobId), ct);

        return (await new GetRequiredProfileHandler(db).Handle(new GetRequiredProfileQuery(cmd.JobId), ct));
    }
}

internal static class SetRequiredOverrideEndpoint
{
    public static void MapSetRequiredOverrideEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/jobs/{jobId:guid}/required-profile", async (Guid jobId, SetRequiredOverrideRequest body, SetRequiredOverrideHandler h, CancellationToken ct) =>
            (await h.Handle(new SetRequiredOverrideCommand(jobId, body.CompetencyId, body.RequiredLevel), ct)).ToHttp());
    }
}
