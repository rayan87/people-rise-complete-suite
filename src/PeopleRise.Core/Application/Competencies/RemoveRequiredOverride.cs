using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

// Reverts a job to plain template inheritance for one competency (deletes the override row, as
// opposed to SetRequiredOverride(..., RequiredLevel: null) which EXCLUDES it).
public sealed record RemoveRequiredOverrideCommand(Guid JobId, Guid CompetencyId);

internal sealed class RemoveRequiredOverrideHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<RemoveRequiredOverrideCommand, Result<RequiredProfileDto>>
{
    public async Task<Result<RequiredProfileDto>> Handle(RemoveRequiredOverrideCommand cmd, CancellationToken ct)
    {
        var existing = await db.RequiredCompetencyOverrides
            .FirstOrDefaultAsync(o => o.JobId == cmd.JobId && o.CompetencyId == cmd.CompetencyId, ct);
        if (existing is null) return Error.NotFound("No override to remove for this job and competency.");

        db.RequiredCompetencyOverrides.Remove(existing);
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new RequiredProfileChanged(cmd.JobId), ct);

        return (await new GetRequiredProfileHandler(db).Handle(new GetRequiredProfileQuery(cmd.JobId), ct));
    }
}

internal static class RemoveRequiredOverrideEndpoint
{
    public static void MapRemoveRequiredOverrideEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/jobs/{jobId:guid}/required-profile/{competencyId:guid}",
            async (Guid jobId, Guid competencyId, RemoveRequiredOverrideHandler h, CancellationToken ct) =>
                (await h.Handle(new RemoveRequiredOverrideCommand(jobId, competencyId), ct)).ToHttp());
    }
}
