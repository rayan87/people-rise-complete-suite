using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Application.Grades;
using PeopleRise.Core.Application.SalaryBands;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

public sealed record GenerateBandsRequest(decimal BaseMidpoint, decimal ProgressionPct, string Currency, DateOnly EffectiveDate);

/// <summary>The Salary Builder's core action: build a band for every grade from a base midpoint and a
/// grade-progression rate - Compensation's own algorithm. SalaryBand itself lives in Core (Core Spec
/// §9), so this only computes each grade's target midpoint and calls Core's public write path
/// (SetDesignedBandCommand, provenance Designed) once per grade - Core owns persistence, the band
/// arithmetic (spread/overlap derivation), and provenance handling.</summary>
public sealed record GenerateBandsCommand(decimal BaseMidpoint, decimal ProgressionPct, string Currency, DateOnly EffectiveDate);

internal sealed class GenerateBandsHandler(
    IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>> listGrades,
    ICommandHandler<SetDesignedBandCommand, Result<SalaryBandRowDto>> setDesignedBand,
    IQueryHandler<ListSalaryBandsQuery, Result<IReadOnlyList<SalaryBandRowDto>>> listSalaryBands)
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

        var gradesResult = await listGrades.Handle(new ListGradesQuery(), ct);
        if (gradesResult.IsFailure) return gradesResult.Error!;
        var grades = gradesResult.Value.OrderBy(g => g.Rank).ToList();

        if (grades.Count == 0)
        {
            return Error.Validation("There are no grades to build bands for.");
        }

        decimal? previousMidpoint = null;
        foreach (var grade in grades)
        {
            // The first grade seeds from the base midpoint; every grade after climbs by the
            // progression rate off the PREVIOUS (already-rounded) midpoint, rounded to the nearest 100.
            var raw = previousMidpoint is { } prev ? prev * (1m + cmd.ProgressionPct / 100m) : cmd.BaseMidpoint;
            var midpoint = Math.Round(raw / 100m, MidpointRounding.AwayFromZero) * 100m;

            var setResult = await setDesignedBand.Handle(
                new SetDesignedBandCommand(grade.Id, cmd.Currency, midpoint, previousMidpoint, cmd.EffectiveDate), ct);
            if (setResult.IsFailure) return setResult.Error!;

            previousMidpoint = midpoint;
        }

        return await listSalaryBands.Handle(new ListSalaryBandsQuery(), ct);
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
