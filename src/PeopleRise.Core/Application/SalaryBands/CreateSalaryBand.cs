using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Application.Identity;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.SalaryBands;

// Core UI's direct-entry path - provenance is always ManuallyEntered (Compensation's generated
// path is SetDesignedBandCommand, provenance Designed). Three mutually exclusive ways to size the
// band (Core Spec §9): Midpoint (+ optional HalfSpreadPct, defaulting 25%) with Min/Max derived
// symmetrically; OverlapPct, deriving Midpoint from the previous grade's midpoint; or MinAmount +
// MaxAmount entered directly, with Midpoint (their arithmetic mean) and the effective half-spread
// derived read-only from them - "bands exist without Compensation".
public sealed record CreateSalaryBandCommand(
    Guid GradeId, string Currency, DateOnly EffectiveDate,
    decimal? Midpoint = null, decimal? OverlapPct = null, decimal? HalfSpreadPct = null,
    decimal? MinAmount = null, decimal? MaxAmount = null);

internal sealed class CreateSalaryBandHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<CreateSalaryBandCommand, Result<SalaryBandRowDto>>
{
    public async Task<Result<SalaryBandRowDto>> Handle(CreateSalaryBandCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Currency))
        {
            return Error.Validation("Currency is required.");
        }

        var modeCount = (cmd.Midpoint is not null ? 1 : 0) + (cmd.OverlapPct is not null ? 1 : 0)
            + (cmd.MinAmount is not null || cmd.MaxAmount is not null ? 1 : 0);
        if (modeCount != 1)
        {
            return Error.Validation("Provide exactly one of Midpoint, OverlapPct, or Min/Max amounts.");
        }

        if ((cmd.MinAmount is null) != (cmd.MaxAmount is null))
        {
            return Error.Validation("Provide both MinAmount and MaxAmount together.");
        }

        if (cmd.MinAmount is not null && cmd.HalfSpreadPct is not null)
        {
            return Error.Validation("Half-spread is derived when entering Min/Max directly, not an input.");
        }

        var grade = await db.Grades.FirstOrDefaultAsync(g => g.Id == cmd.GradeId, ct);
        if (grade is null)
        {
            return Error.NotFound("Grade not found.");
        }

        if (await db.SalaryBands.AnyAsync(b => b.GradeId == cmd.GradeId && b.JobFamilyId == null, ct))
        {
            return Error.Conflict("This grade already has a band; update it instead.");
        }

        var previousMidpoint = await SalaryBandProjections.PreviousMidpointAsync(db, grade.Rank, ct);

        if (cmd.OverlapPct is not null && previousMidpoint is null)
        {
            return Error.Validation("This is the first grade; there is no previous midpoint to derive an overlap from.");
        }

        decimal midpoint;
        decimal? halfSpreadPct = cmd.HalfSpreadPct;

        if (cmd.MinAmount is { } min && cmd.MaxAmount is { } max)
        {
            if (min <= 0 || max <= min)
            {
                return Error.Validation("MaxAmount must be greater than MinAmount, and both must be positive.");
            }
            midpoint = (min + max) / 2m;                              // Core Spec §9 band arithmetic
            halfSpreadPct = (max - midpoint) / midpoint * 100m;       // derived read-only
        }
        else
        {
            midpoint = cmd.Midpoint ?? previousMidpoint!.Value * (1m + cmd.OverlapPct!.Value / 100m);
        }

        if (midpoint <= 0)
        {
            return Error.Validation("Midpoint must be greater than zero.");
        }

        var salaryBand = halfSpreadPct is { } hs
            ? SalaryBand.Create(cmd.GradeId, cmd.Currency, midpoint, previousMidpoint, cmd.EffectiveDate, BandProvenance.ManuallyEntered, halfSpreadPct: hs)
            : SalaryBand.Create(cmd.GradeId, cmd.Currency, midpoint, previousMidpoint, cmd.EffectiveDate, BandProvenance.ManuallyEntered);

        db.SalaryBands.Add(salaryBand);
        await SalaryBandProjections.CascadeMidpointsAsync(db, grade.Rank, midpoint, ct);
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new BandPublished(cmd.GradeId, salaryBand.Id), ct);
        return (await SalaryBandProjections.RowForGradeAsync(db, cmd.GradeId, ct))!;
    }
}

internal static class CreatSalaryBandEndpoint
{
    public static void MapCreatSalaryBandEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateSalaryBandRequest body, CreateSalaryBandHandler h, CancellationToken ct) =>
            (await h.Handle(new CreateSalaryBandCommand(
                body.GradeId, body.Currency, body.EffectiveDate, body.Midpoint, body.OverlapPct, body.HalfSpreadPct,
                body.MinAmount, body.MaxAmount), ct)).ToHttp())
            .RequirePermission(Permission.ManageSensitiveData);
    }
}
