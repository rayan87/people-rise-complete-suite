using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;

namespace PeopleRise.Core.Application.Jobs;

// Job's current grade is a query against JobGradeAssignment, not a column on Job (Core Spec
// §3.2b/§6 - effective-dated). Left-joined in here so GetJob/ListJobs share one shape, the same
// "mixed query" style the core extraction already established elsewhere in this module. Filtering
// and ordering happen on the raw Job queryable BEFORE the join/projection into JobDto - EF Core
// can't translate a Where/OrderBy applied on top of an already-projected record shape.
internal static class JobProjections
{
    public static Task<JobDto?> ByIdAsync(CoreDbContext db, Guid id, CancellationToken ct) =>
        Project(db, db.Jobs.Where(j => j.Id == id)).FirstOrDefaultAsync(ct);

    // Archived jobs are excluded from pickers by default; historical views pass includeClosed
    // (Core Spec §3.4).
    public static Task<List<JobDto>> ListAsync(CoreDbContext db, bool includeClosed, CancellationToken ct)
    {
        var jobs = db.Jobs.AsQueryable();
        if (!includeClosed) jobs = jobs.Where(j => j.Status != JobStatus.Archived);
        jobs = jobs.OrderBy(j => j.Code);
        return Project(db, jobs).ToListAsync(ct);
    }

    private static IQueryable<JobDto> Project(CoreDbContext db, IQueryable<Job> jobs) =>
        from j in jobs
        join ga in db.JobGradeAssignments.Where(a => a.EndDate == null) on j.Id equals ga.JobId into gaj
        from ga in gaj.DefaultIfEmpty()
        select new JobDto(
            j.Id, j.Code, j.TitleEn, j.TitleAr, j.DescriptionEn, j.DescriptionAr,
            j.JobFamilyId, j.JobFamily!.Code, j.JobFamily.NameEn, j.JobFamily.NameAr,
            ga == null ? null : (Guid?)ga.GradeId,
            ga == null ? null : ga.Grade!.Code,
            ga == null ? null : ga.Grade!.NameEn,
            ga == null ? null : ga.Grade!.NameAr,
            ga == null ? null : (Guid?)ga.Grade!.LevelId,
            ga == null ? null : ga.Grade!.Level!.Code,
            ga == null ? null : ga.Grade!.Level!.NameEn,
            ga == null ? null : ga.Grade!.Level!.NameAr,
            j.Status.ToString(),
            ga == null ? null : ga.Source.ToString(),
            ga == null ? null :
                db.SalaryBands.Where(b => b.GradeId == ga.GradeId && b.JobFamilyId == null)
                    .Select(b => new JobBandDto(b.Currency, b.MinAmount, b.Midpoint, b.MaxAmount)).FirstOrDefault()
        );
}
