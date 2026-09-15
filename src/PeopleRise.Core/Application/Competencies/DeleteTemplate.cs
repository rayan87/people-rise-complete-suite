using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

public sealed record DeleteTemplateCommand(Guid Id);

// Templates aren't referenced by anything outside themselves (jobs inherit by LevelId/JobFamilyId
// lookup, not by a stored link - Core Spec §10), so deleting one is always a plain delete; nothing
// is orphaned, jobs in that cell simply stop finding a template (and the level-only fallback, if
// one exists, covers the family-specific case's fallback path).
internal sealed class DeleteTemplateHandler(CoreDbContext db)
    : ICommandHandler<DeleteTemplateCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteTemplateCommand cmd, CancellationToken ct)
    {
        var template = await db.CompetencyTemplates.FindAsync(cmd.Id, ct);
        if (template is null) return Error.NotFound("Template not found.");

        var items = await db.CompetencyTemplateItems.Where(i => i.TemplateId == cmd.Id).ToListAsync(ct);
        db.CompetencyTemplateItems.RemoveRange(items);
        db.CompetencyTemplates.Remove(template);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteTemplateEndpoint
{
    public static void MapDeleteTemplateEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteTemplateHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteTemplateCommand(id), ct)).ToHttp());
    }
}
