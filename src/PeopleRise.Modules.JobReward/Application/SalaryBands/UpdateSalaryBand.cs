using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

public sealed record UpdateSalaryBandCommand(Guid BandId, string Currency, decimal? Midpoint, decimal? OverlapPct, DateOnly EffectiveDate);

internal sealed class UpdateSalaryBandHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateSalaryBandCommand, Result<SalaryBandRowDto>>
{
    public async Task<Result<SalaryBandRowDto>> Handle(UpdateSalaryBandCommand cmd, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cmd.Currency))
        {
            return Error.Validation("Currency is required.");
        }

        if (cmd.Midpoint is null == cmd.OverlapPct is null)
        {
            return Error.Validation("Provide exactly one of Midpoint or OverlapPct.");
        }

        var band = await db.SalaryBands
            .FirstOrDefaultAsync(b => b.Id == cmd.BandId, cancellationToken);

        if (band is null)
        {
            return Error.NotFound("Salary band not found.");
        }

        var rank = await db.Grades
            .Where(g => g.Id == band.GradeId)
            .Select(g => g.Rank)
            .FirstAsync(cancellationToken);
        var previousMidpoint = await SalaryBandProjections.PreviousMidpointAsync(db, rank, cancellationToken);

        if (cmd.OverlapPct is not null && previousMidpoint is null)
        {
            return Error.Validation("This is the first grade; there is no previous midpoint to derive an overlap from.");
        }

        var midpoint = cmd.Midpoint ?? previousMidpoint!.Value * (1m + cmd.OverlapPct!.Value / 100m);

        if (midpoint <= 0)
        {
            return Error.Validation("Midpoint must be greater than zero.");
        }

        band.Update(midpoint, previousMidpoint, cmd.Currency, cmd.EffectiveDate);
        await SalaryBandProjections.CascadeMidpointsAsync(db, rank, midpoint, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return (await SalaryBandProjections.RowForGradeAsync(db, band.GradeId, cancellationToken))!;
    }
}

internal static class UpdateSalaryBandEndpoint
{
    public static void MapUpdateSalaryBandEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateSalaryBandRequest body, UpdateSalaryBandHandler h, CancellationToken ct) =>
            (await h.Handle(new UpdateSalaryBandCommand(
                id, body.Currency, body.Midpoint, body.OverlapPct, body.EffectiveDate), ct)).ToHttp());
    }
}
