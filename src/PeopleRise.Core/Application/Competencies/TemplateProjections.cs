using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;

namespace PeopleRise.Core.Application.Competencies;

internal static class TemplateProjections
{
    public static async Task<CompetencyTemplateDto?> ByIdAsync(CoreDbContext db, Guid id, CancellationToken ct)
    {
        var template = await db.CompetencyTemplates.Where(t => t.Id == id)
            .Select(t => new { t.Id, t.LevelId, LevelCode = t.Level!.Code, t.JobFamilyId, JobFamilyCode = t.JobFamily != null ? t.JobFamily.Code : null })
            .FirstOrDefaultAsync(ct);
        if (template is null) return null;

        var items = await db.CompetencyTemplateItems.Where(i => i.TemplateId == id)
            .Select(i => new TemplateItemDto(i.CompetencyId, i.Competency!.Code, i.Competency.NameEn, i.Competency.NameAr, i.RequiredLevel))
            .ToListAsync(ct);

        return new CompetencyTemplateDto(template.Id, template.LevelId, template.LevelCode, template.JobFamilyId, template.JobFamilyCode, items);
    }
}
