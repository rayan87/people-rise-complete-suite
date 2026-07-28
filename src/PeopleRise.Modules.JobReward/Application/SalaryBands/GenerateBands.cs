using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

/// <summary>The Salary Builder's core action: build a band for every grade from a base midpoint and a
/// grade-progression rate. Spread is a fixed ±25% of midpoint (see SalaryBand); overlap per grade is
/// derived from consecutive midpoints. Upserts (updates existing grade bands).</summary>
public sealed record GenerateBandsCommand(decimal BaseMidpoint, decimal ProgressionPct, string Currency, DateOnly EffectiveDate);

internal sealed class GenerateBandsHandler(JobRewardDbContext db)
    : ICommandHandler<GenerateBandsCommand, Result<IReadOnlyList<SalaryBandRowDto>>>
{
    public async Task<Result<IReadOnlyList<SalaryBandRowDto>>> Handle(GenerateBandsCommand cmd, CancellationToken ct)
    {
        if (cmd.BaseMidpoint <= 0)
        {
            return Error.Validation("Base midpoint must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(cmd.Currency))
        {
            return Error.Validation("Currency is required.");
        }

        var grades = await db.Grades
            .OrderBy(g => g.Rank)
            .ToListAsync(ct);

        if (grades.Count == 0)
        {
            return Error.Validation("There are no grades to build bands for.");
        }

        var existing = (await db.SalaryBands.Where(b => b.JobFamilyId == null).ToListAsync(ct))
            .ToDictionary(b => b.GradeId);

        decimal? previousMidpoint = null;
        for (var i = 0; i < grades.Count; i++)
        {
            // The first grade seeds from the base midpoint; every grade after climbs by the
            // progression rate off the PREVIOUS (already-rounded) midpoint, rounded to the nearest 100.
            var raw = previousMidpoint is { } prev ? prev * (1m + cmd.ProgressionPct / 100m) : cmd.BaseMidpoint;
            var midpoint = Math.Round(raw / 100m, MidpointRounding.AwayFromZero) * 100m;

            if (existing.TryGetValue(grades[i].Id, out var band))
                band.Update(midpoint, previousMidpoint, cmd.Currency, cmd.EffectiveDate);
            else
                db.SalaryBands.Add(SalaryBand.Create(
                    grades[i].Id, cmd.Currency, midpoint, previousMidpoint, cmd.EffectiveDate));

            previousMidpoint = midpoint;
        }

        await db.SaveChangesAsync(ct);
        var rows = await SalaryBandProjections.RowsAsync(db, ct);
        return Result<IReadOnlyList<SalaryBandRowDto>>.Success(rows);
    }
}

internal static class GenerateBandsEndpoint
{
    public static void MapGenerateBandsEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/generate", async (GenerateBandsRequest body, GenerateBandsHandler h, CancellationToken ct) =>
             (await h.Handle(new GenerateBandsCommand(
                 body.BaseMidpoint, body.ProgressionPct, body.Currency, body.EffectiveDate), ct)).ToHttp());
    }
}
