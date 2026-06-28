using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

/// <summary>Projects grades (each with its grade-level band, if any) — the Salary Builder's view.
/// Ordering/filtering is applied to the grade source BEFORE projecting (EF can't order on a
/// projected DTO that carries a subquery).</summary>
internal static class SalaryBandProjections
{
    public static Task<List<SalaryBandRowDto>> RowsAsync(JobRewardDbContext db, CancellationToken ct) =>
        Project(db, db.Grades.OrderBy(g => g.Rank)).ToListAsync(ct);

    public static Task<SalaryBandRowDto?> RowForGradeAsync(JobRewardDbContext db, Guid gradeId, CancellationToken ct) =>
        Project(db, db.Grades.Where(g => g.Id == gradeId)).FirstOrDefaultAsync(ct);

    private static IQueryable<SalaryBandRowDto> Project(JobRewardDbContext db, IQueryable<Grade> grades) =>
        grades.Select(g => new SalaryBandRowDto(
            g.Id, g.Code, g.NameEn, g.NameAr, g.Rank, g.Level!.Code,
            db.SalaryBands.Where(b => b.GradeId == g.Id && b.JobFamilyId == null)
                .Select(b => new SalaryBandInfo(
                    b.Id, b.Currency, b.MinAmount, b.Midpoint, b.MaxAmount,
                    b.SpreadPct, b.OverlapPct, b.EffectiveDate, b.Status.ToString()))
                .FirstOrDefault()));
}
