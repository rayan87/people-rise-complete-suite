using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

// One template per (level, family) cell - JobFamilyId null is the level-only fallback that applies
// while a job has no family (Core Spec §10).
public sealed record CreateTemplateCommand(Guid LevelId, Guid? JobFamilyId, IReadOnlyList<(Guid CompetencyId, int RequiredLevel)> Items);

internal sealed class CreateTemplateHandler(CoreDbContext db)
    : ICommandHandler<CreateTemplateCommand, Result<CompetencyTemplateDto>>
{
    public async Task<Result<CompetencyTemplateDto>> Handle(CreateTemplateCommand cmd, CancellationToken ct)
    {
        if (!await db.Levels.AnyAsync(l => l.Id == cmd.LevelId, ct))
        {
            return Error.NotFound("Level not found.");
        }

        if (cmd.JobFamilyId is { } familyId && !await db.JobFamilies.AnyAsync(f => f.Id == familyId, ct))
        {
            return Error.NotFound("Job family not found.");
        }

        var exists = cmd.JobFamilyId is null
            ? await db.CompetencyTemplates.AnyAsync(t => t.LevelId == cmd.LevelId && t.JobFamilyId == null, ct)
            : await db.CompetencyTemplates.AnyAsync(t => t.LevelId == cmd.LevelId && t.JobFamilyId == cmd.JobFamilyId, ct);
        if (exists)
        {
            return Error.Conflict("A template already exists for this cell; update it instead.");
        }

        foreach (var item in cmd.Items)
        {
            if (item.RequiredLevel is < 1 or > 5)
                return Error.Validation("RequiredLevel must be between 1 and 5.");
        }

        var competencyIds = cmd.Items.Select(i => i.CompetencyId).ToList();
        var knownCount = await db.CompetencyDefinitions.CountAsync(c => competencyIds.Contains(c.Id), ct);
        if (knownCount != competencyIds.Distinct().Count())
        {
            return Error.Validation("One or more competencies were not found.");
        }

        var template = CompetencyTemplate.Create(cmd.LevelId, cmd.JobFamilyId);
        db.CompetencyTemplates.Add(template);
        foreach (var item in cmd.Items)
        {
            db.CompetencyTemplateItems.Add(CompetencyTemplateItem.Create(template.Id, item.CompetencyId, item.RequiredLevel));
        }
        await db.SaveChangesAsync(ct);

        return (await TemplateProjections.ByIdAsync(db, template.Id, ct))!;
    }
}

internal static class CreateTemplateEndpoint
{
    public static void MapCreateTemplateEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateTemplateRequest body, CreateTemplateHandler h, CancellationToken ct) =>
            (await h.Handle(new CreateTemplateCommand(body.LevelId, body.JobFamilyId,
                body.Items.Select(i => (i.CompetencyId, i.RequiredLevel)).ToList()), ct)).ToHttp());
    }
}
