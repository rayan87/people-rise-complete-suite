using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Grades;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

/// <summary>Projects grades (each with its grade-level band, if any) — the Salary Builder's view.
/// Grade lives in PeopleRise.Core; this always fetches the full grade list via its public contract
/// and zips it against this module's own SalaryBand rows in C#, since the two can no longer be
/// joined in one query.</summary>
internal static class SalaryBandProjections
{
    public static async Task<List<SalaryBandRowDto>> RowsAsync(
        JobRewardDbContext db, IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>> listGrades, CancellationToken ct)
    {
        var grades = await GradesByRankAsync(listGrades, ct);
        return await ProjectAsync(db, grades, ct);
    }

    public static async Task<SalaryBandRowDto?> RowForGradeAsync(
        JobRewardDbContext db, IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>> listGrades, Guid gradeId, CancellationToken ct)
    {
        var grades = await GradesByRankAsync(listGrades, ct);
        var grade = grades.FirstOrDefault(g => g.Id == gradeId);
        if (grade is null) return null;
        var rows = await ProjectAsync(db, [grade], ct);
        return rows.FirstOrDefault();
    }

    /// <summary>The grade-level band midpoint of the previous grade by Rank, or null if this is the
    /// first grade or that grade has no band yet.</summary>
    public static async Task<decimal?> PreviousMidpointAsync(
        JobRewardDbContext db, IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>> listGrades, int rank, CancellationToken ct)
    {
        var grades = await GradesByRankAsync(listGrades, ct);
        var previousGradeId = grades.Where(g => g.Rank < rank).Select(g => (Guid?)g.Id).LastOrDefault();

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
    public static async Task CascadeMidpointsAsync(
        JobRewardDbContext db, IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>> listGrades,
        int editedRank, decimal newMidpoint, CancellationToken ct)
    {
        var grades = await GradesByRankAsync(listGrades, ct);
        var followingGradeIds = grades.Where(g => g.Rank > editedRank).Select(g => g.Id).ToList();

        var previousMidpoint = newMidpoint;
        foreach (var gradeId in followingGradeIds)
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

    private static async Task<List<GradeDto>> GradesByRankAsync(
        IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>> listGrades, CancellationToken ct)
    {
        var result = await listGrades.Handle(new ListGradesQuery(), ct);
        return result.IsSuccess ? result.Value.OrderBy(g => g.Rank).ToList() : [];
    }

    private static async Task<List<SalaryBandRowDto>> ProjectAsync(JobRewardDbContext db, IReadOnlyList<GradeDto> grades, CancellationToken ct)
    {
        var gradeIds = grades.Select(g => g.Id).ToList();
        var bandsByGradeId = (await db.SalaryBands
                .Where(b => gradeIds.Contains(b.GradeId) && b.JobFamilyId == null)
                .ToListAsync(ct))
            .ToDictionary(b => b.GradeId);

        return grades.Select(g =>
        {
            var band = bandsByGradeId.GetValueOrDefault(g.Id);
            var info = band is null ? null : new SalaryBandInfo(
                band.Id, band.Currency, band.MinAmount, band.Midpoint, band.MaxAmount,
                band.HalfSpreadPct, band.SpreadPct, band.OverlapPct, band.EffectiveDate,
                band.Status.ToString(), band.Provenance.ToString());
            return new SalaryBandRowDto(g.Id, g.Code, g.NameEn, g.NameAr, g.Rank, g.LevelCode, info);
        }).ToList();
    }
}
