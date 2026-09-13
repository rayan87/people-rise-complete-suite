using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Grades;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

// Provenance ManuallyEntered - this is a direct entry through the Salary Builder's own form, not
// a Compensation-computed band (see GenerateBands for that path, provenance Designed).
public sealed record CreateSalaryBandCommand(Guid GradeId, string Currency, decimal? Midpoint, decimal? OverlapPct, DateOnly EffectiveDate, decimal? HalfSpreadPct = null);

internal sealed class CreateSalaryBandHandler(
    JobRewardDbContext db, IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>> listGrades)
    : ICommandHandler<CreateSalaryBandCommand, Result<SalaryBandRowDto>>
{
    public async Task<Result<SalaryBandRowDto>> Handle(CreateSalaryBandCommand cmd, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cmd.Currency))
        {
            return Error.Validation("Currency is required.");
        }

        if (cmd.Midpoint is null == cmd.OverlapPct is null)
        {
            return Error.Validation("Provide exactly one of Midpoint or OverlapPct.");
        }

        var gradesResult = await listGrades.Handle(new ListGradesQuery(), cancellationToken);
        if (gradesResult.IsFailure) return gradesResult.Error!;
        var grade = gradesResult.Value.FirstOrDefault(g => g.Id == cmd.GradeId);
        if (grade is null)
        {
            return Error.NotFound("Grade not found.");
        }

        if (await db.SalaryBands.AnyAsync(b => b.GradeId == cmd.GradeId && b.JobFamilyId == null, cancellationToken))
        {
            return Error.Conflict("This grade already has a band; update it instead.");
        }

        var previousMidpoint = await SalaryBandProjections.PreviousMidpointAsync(db, listGrades, grade.Rank, cancellationToken);

        if (cmd.OverlapPct is not null && previousMidpoint is null)
        {
            return Error.Validation("This is the first grade; there is no previous midpoint to derive an overlap from.");
        }

        var midpoint = cmd.Midpoint ?? previousMidpoint!.Value * (1m + cmd.OverlapPct!.Value / 100m);

        if (midpoint <= 0)
        {
            return Error.Validation("Midpoint must be greater than zero.");
        }

        var salaryBand = cmd.HalfSpreadPct is { } halfSpread
            ? SalaryBand.Create(cmd.GradeId, cmd.Currency, midpoint, previousMidpoint, cmd.EffectiveDate, BandProvenance.ManuallyEntered, halfSpreadPct: halfSpread)
            : SalaryBand.Create(cmd.GradeId, cmd.Currency, midpoint, previousMidpoint, cmd.EffectiveDate, BandProvenance.ManuallyEntered);

        db.SalaryBands.Add(salaryBand);
        await SalaryBandProjections.CascadeMidpointsAsync(db, listGrades, grade.Rank, midpoint, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return (await SalaryBandProjections.RowForGradeAsync(db, listGrades, cmd.GradeId, cancellationToken))!;
    }
}

internal static class CreatSalaryBandEndpoint
{
    public static void MapCreatSalaryBandEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateSalaryBandRequest body, CreateSalaryBandHandler h, CancellationToken ct) =>
            (await h.Handle(new CreateSalaryBandCommand(
                body.GradeId, body.Currency, body.Midpoint, body.OverlapPct, body.EffectiveDate), ct)).ToHttp());
    }
}