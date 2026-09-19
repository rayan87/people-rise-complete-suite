using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;

namespace PeopleRise.Core.Application.CareerPaths;

internal static class CareerPathProjections
{
    public static async Task<CareerPathDto?> ByIdAsync(CoreDbContext db, Guid id, CancellationToken ct)
    {
        var path = await db.CareerPaths.Where(p => p.Id == id)
            .Select(p => new { p.Id, p.JobFamilyId, JobFamilyCode = p.JobFamily!.Code, p.NameEn, p.NameAr })
            .FirstOrDefaultAsync(ct);
        if (path is null) return null;

        var steps = await db.CareerPathSteps.Where(s => s.CareerPathId == id)
            .OrderBy(s => s.StepOrder)
            .Select(s => new CareerPathStepDto(s.GradeId, s.Grade!.Code, s.Grade.NameEn, s.Grade.NameAr, s.StepOrder))
            .ToListAsync(ct);

        return new CareerPathDto(path.Id, path.JobFamilyId, path.JobFamilyCode, path.NameEn, path.NameAr, steps);
    }
}
