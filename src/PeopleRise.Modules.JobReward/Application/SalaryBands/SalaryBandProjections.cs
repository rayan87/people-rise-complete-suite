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

    /// <summary>The grade-level band midpoint of the previous grade by Rank, or null if this is the
    /// first grade or that grade has no band yet.</summary>
    public static async Task<decimal?> PreviousMidpointAsync(JobRewardDbContext db, int rank, CancellationToken ct)
    {
        var previousGradeId = await db.Grades
            .Where(g => g.Rank < rank)
            .OrderByDescending(g => g.Rank)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(ct);

        return previousGradeId is null
            ? null
            : await db.SalaryBands
                .Where(b => b.GradeId == previousGradeId && b.JobFamilyId == null)
                .Select(b => (decimal?)b.Midpoint)
                .FirstOrDefaultAsync(ct);
    }

    /// <summary>After a grade's midpoint changes, ripple that change upward: each following grade (by
    /// Rank) keeps its OWN stored OverlapPct fixed and gets its midpoint re-derived from the new
    /// midpoint below it. Stops at the first grade with no existing band (nothing to cascade into) —
    /// a gap breaks the chain, same as a null OverlapPct would.</summary>
    public static async Task CascadeMidpointsAsync(JobRewardDbContext db, int editedRank, decimal newMidpoint, CancellationToken ct)
    {
        var followingGrades = await db.Grades
            .Where(g => g.Rank > editedRank)
            .OrderBy(g => g.Rank)
            .Select(g => g.Id)
            .ToListAsync(ct);

        var previousMidpoint = newMidpoint;
        foreach (var gradeId in followingGrades)
        {
            var band = await db.SalaryBands
                .FirstOrDefaultAsync(b => b.GradeId == gradeId && b.JobFamilyId == null, ct);

            if (band?.OverlapPct is not { } overlap)
            {
                break;
            }

            var midpoint = previousMidpoint * (1m + overlap / 100m);
            band.Update(midpoint, previousMidpoint, band.Currency, band.EffectiveDate);
            previousMidpoint = midpoint;
        }
    }

    private static IQueryable<SalaryBandRowDto> Project(JobRewardDbContext db, IQueryable<Grade> grades) =>
        grades.Select(g => new SalaryBandRowDto(
            g.Id, g.Code, g.NameEn, g.NameAr, g.Rank, g.Level!.Code,
            db.SalaryBands.Where(b => b.GradeId == g.Id && b.JobFamilyId == null)
                .Select(b => new SalaryBandInfo(
                    b.Id, b.Currency, b.MinAmount, b.Midpoint, b.MaxAmount,
                    b.MinAmount == 0 ? 0m : (b.MaxAmount / b.MinAmount - 1m) * 100m,
                    b.OverlapPct, b.EffectiveDate, b.Status.ToString()))
                .FirstOrDefault()));
}
