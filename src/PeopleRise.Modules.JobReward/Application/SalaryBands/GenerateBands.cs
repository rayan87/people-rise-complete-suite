using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

/// <summary>The Salary Builder's core action: build a band for every grade from a base midpoint,
/// a spread, and a midpoint-to-midpoint progression. Upserts (updates existing grade bands).</summary>
public sealed record GenerateBandsCommand(decimal BaseMidpoint, decimal SpreadPct, decimal ProgressionPct, string Currency, DateOnly EffectiveDate);

internal sealed class GenerateBandsHandler(JobRewardDbContext db)
    : ICommandHandler<GenerateBandsCommand, Result<IReadOnlyList<SalaryBandRowDto>>>
{
    public async Task<Result<IReadOnlyList<SalaryBandRowDto>>> Handle(GenerateBandsCommand cmd, CancellationToken ct)
    {
        if (cmd.BaseMidpoint <= 0) return Error.Validation("Base midpoint must be greater than zero.");
        if (string.IsNullOrWhiteSpace(cmd.Currency)) return Error.Validation("Currency is required.");

        var grades = await db.Grades.OrderBy(g => g.Rank).ToListAsync(ct);
        if (grades.Count == 0) return Error.Validation("There are no grades to build bands for.");

        var existing = (await db.SalaryBands.Where(b => b.JobFamilyId == null).ToListAsync(ct))
            .ToDictionary(b => b.GradeId);
        var factor = 1m + cmd.ProgressionPct / 100m;

        for (var i = 0; i < grades.Count; i++)
        {
            // midpoint climbs by the progression each grade, rounded to the nearest 100.
            var raw = cmd.BaseMidpoint * (decimal)Math.Pow((double)factor, i);
            var midpoint = Math.Round(raw / 100m, MidpointRounding.AwayFromZero) * 100m;

            if (existing.TryGetValue(grades[i].Id, out var band))
                band.Update(midpoint, cmd.SpreadPct, cmd.ProgressionPct, cmd.Currency, cmd.EffectiveDate);
            else
                db.SalaryBands.Add(SalaryBand.Create(
                    grades[i].Id, cmd.Currency, midpoint, cmd.SpreadPct, cmd.ProgressionPct, cmd.EffectiveDate));
        }

        await db.SaveChangesAsync(ct);
        var rows = await SalaryBandProjections.RowsAsync(db, ct);
        return Result<IReadOnlyList<SalaryBandRowDto>>.Success(rows);
    }
}
