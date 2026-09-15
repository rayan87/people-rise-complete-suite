using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

public sealed record GetRequiredProfileQuery(Guid JobId);

// Resolves a job's effective required profile: its family-and-level cell's template, merged with
// its own overrides (Core Spec §10 - "jobs inherit their cell template and may override individual
// competencies"). A job's level comes from its current grade (Core Spec §5/§6's shared coordinate
// system) - a job with no grade yet resolves to an empty profile, since there's no cell to look up.
internal sealed class GetRequiredProfileHandler(CoreDbContext db)
    : IQueryHandler<GetRequiredProfileQuery, Result<RequiredProfileDto>>
{
    public async Task<Result<RequiredProfileDto>> Handle(GetRequiredProfileQuery query, CancellationToken ct)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == query.JobId, ct);
        if (job is null) return Error.NotFound("Job not found.");

        var levelId = await db.JobGradeAssignments.Where(a => a.JobId == job.Id && a.EndDate == null)
            .Select(a => (Guid?)a.Grade!.LevelId).FirstOrDefaultAsync(ct);
        if (levelId is null) return new RequiredProfileDto(job.Id, []);

        var template = await db.CompetencyTemplates.FirstOrDefaultAsync(t => t.LevelId == levelId && t.JobFamilyId == job.JobFamilyId, ct)
            ?? await db.CompetencyTemplates.FirstOrDefaultAsync(t => t.LevelId == levelId && t.JobFamilyId == null, ct);

        var levels = template is null
            ? new Dictionary<Guid, int>()
            : await db.CompetencyTemplateItems.Where(i => i.TemplateId == template.Id)
                .ToDictionaryAsync(i => i.CompetencyId, i => i.RequiredLevel, ct);

        var overriddenIds = new HashSet<Guid>();
        var overrides = await db.RequiredCompetencyOverrides.Where(o => o.JobId == job.Id).ToListAsync(ct);
        foreach (var o in overrides)
        {
            overriddenIds.Add(o.CompetencyId);
            if (o.RequiredLevel is { } level) levels[o.CompetencyId] = level;
            else levels.Remove(o.CompetencyId);
        }

        if (levels.Count == 0) return new RequiredProfileDto(job.Id, []);

        var competencies = await db.CompetencyDefinitions.Where(c => levels.Keys.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var items = levels.Select(kv => new RequiredCompetencyItemDto(
            kv.Key, competencies[kv.Key].Code, competencies[kv.Key].NameEn, competencies[kv.Key].NameAr,
            kv.Value, overriddenIds.Contains(kv.Key))).ToList();

        return new RequiredProfileDto(job.Id, items);
    }
}

internal static class GetRequiredProfileEndpoint
{
    public static void MapGetRequiredProfileEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/jobs/{jobId:guid}/required-profile", async (Guid jobId, GetRequiredProfileHandler h, CancellationToken ct) =>
            (await h.Handle(new GetRequiredProfileQuery(jobId), ct)).ToHttp());
    }
}
