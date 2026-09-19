using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Application.Identity;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.SalaryBands;

public sealed record UpdateSalaryBandCommand(
    Guid BandId, string Currency, DateOnly EffectiveDate,
    decimal? Midpoint = null, decimal? OverlapPct = null, decimal? HalfSpreadPct = null,
    decimal? MinAmount = null, decimal? MaxAmount = null);

internal sealed class UpdateSalaryBandHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<UpdateSalaryBandCommand, Result<SalaryBandRowDto>>
{
    public async Task<Result<SalaryBandRowDto>> Handle(UpdateSalaryBandCommand cmd, CancellationToken ct)
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

        var band = await db.SalaryBands
            .FirstOrDefaultAsync(b => b.Id == cmd.BandId, ct);

        if (band is null)
        {
            return Error.NotFound("Salary band not found.");
        }

        var rank = await db.Grades
            .Where(g => g.Id == band.GradeId)
            .Select(g => g.Rank)
            .FirstAsync(ct);
        var previousMidpoint = await SalaryBandProjections.PreviousMidpointAsync(db, rank, ct);

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
            midpoint = (min + max) / 2m;
            halfSpreadPct = (max - midpoint) / midpoint * 100m;
        }
        else
        {
            midpoint = cmd.Midpoint ?? previousMidpoint!.Value * (1m + cmd.OverlapPct!.Value / 100m);
        }

        if (midpoint <= 0)
        {
            return Error.Validation("Midpoint must be greater than zero.");
        }

        band.Update(midpoint, previousMidpoint, cmd.Currency, cmd.EffectiveDate, halfSpreadPct);
        await SalaryBandProjections.CascadeMidpointsAsync(db, rank, midpoint, ct);
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new BandPublished(band.GradeId, band.Id), ct);
        return (await SalaryBandProjections.RowForGradeAsync(db, band.GradeId, ct))!;
    }
}

internal static class UpdateSalaryBandEndpoint
{
    public static void MapUpdateSalaryBandEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateSalaryBandRequest body, UpdateSalaryBandHandler h, CancellationToken ct) =>
            (await h.Handle(new UpdateSalaryBandCommand(
                id, body.Currency, body.EffectiveDate, body.Midpoint, body.OverlapPct, body.HalfSpreadPct,
                body.MinAmount, body.MaxAmount), ct)).ToHttp())
            .RequirePermission(Permission.ManageSensitiveData);
    }
}
