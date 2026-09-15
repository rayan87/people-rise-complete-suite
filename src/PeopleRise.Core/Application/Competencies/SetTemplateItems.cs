using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

// Replaces a template's full item set - jobs inheriting this cell see the change immediately
// (Core Spec §10: templates are never materialized into each job, they're joined in at read time).
public sealed record SetTemplateItemsCommand(Guid TemplateId, IReadOnlyList<(Guid CompetencyId, int RequiredLevel)> Items);

internal sealed class SetTemplateItemsHandler(CoreDbContext db)
    : ICommandHandler<SetTemplateItemsCommand, Result<CompetencyTemplateDto>>
{
    public async Task<Result<CompetencyTemplateDto>> Handle(SetTemplateItemsCommand cmd, CancellationToken ct)
    {
        var template = await db.CompetencyTemplates.FirstOrDefaultAsync(t => t.Id == cmd.TemplateId, ct);
        if (template is null) return Error.NotFound("Template not found.");

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

        var existing = await db.CompetencyTemplateItems.Where(i => i.TemplateId == cmd.TemplateId).ToListAsync(ct);
        db.CompetencyTemplateItems.RemoveRange(existing);
        foreach (var item in cmd.Items)
        {
            db.CompetencyTemplateItems.Add(CompetencyTemplateItem.Create(cmd.TemplateId, item.CompetencyId, item.RequiredLevel));
        }
        await db.SaveChangesAsync(ct);

        return (await TemplateProjections.ByIdAsync(db, cmd.TemplateId, ct))!;
    }
}

internal static class SetTemplateItemsEndpoint
{
    public static void MapSetTemplateItemsEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{templateId:guid}/items", async (Guid templateId, SetTemplateItemsRequest body, SetTemplateItemsHandler h, CancellationToken ct) =>
            (await h.Handle(new SetTemplateItemsCommand(templateId, body.Items.Select(i => (i.CompetencyId, i.RequiredLevel)).ToList()), ct)).ToHttp());
    }
}
