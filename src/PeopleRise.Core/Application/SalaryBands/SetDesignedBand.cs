using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.SalaryBands;

/// <summary>Compensation's write path (Core Spec §11.2: "Pay — salary bands: Core UI · Compensation,
/// ManuallyEntered · Designed" - the same shape as Job Evaluation's AssignJobGradeCommand). Not
/// exposed as an HTTP endpoint - called by JobReward's GenerateBands (the progression-rate
/// algorithm is Compensation's own logic; this only persists the result Core owns).</summary>
public sealed record SetDesignedBandCommand(Guid GradeId, string Currency, decimal Midpoint, decimal? PreviousGradeMidpoint, DateOnly EffectiveDate);

internal sealed class SetDesignedBandHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<SetDesignedBandCommand, Result<SalaryBandRowDto>>
{
    public async Task<Result<SalaryBandRowDto>> Handle(SetDesignedBandCommand cmd, CancellationToken ct)
    {
        var existing = await db.SalaryBands
            .FirstOrDefaultAsync(b => b.GradeId == cmd.GradeId && b.JobFamilyId == null, ct);

        Guid bandId;
        if (existing is null)
        {
            var created = SalaryBand.Create(cmd.GradeId, cmd.Currency, cmd.Midpoint, cmd.PreviousGradeMidpoint, cmd.EffectiveDate, BandProvenance.Designed);
            db.SalaryBands.Add(created);
            bandId = created.Id;
        }
        else if (existing.Provenance == BandProvenance.Designed)
        {
            // Same source re-pricing - update in place.
            existing.Update(cmd.Midpoint, cmd.PreviousGradeMidpoint, cmd.Currency, cmd.EffectiveDate);
            bandId = existing.Id;
        }
        else
        {
            // Different source (a customer's own manual entry) - provenance is never upgraded in
            // place (Core Spec §3.1): retire the old record and create a new one instead of silently
            // relabeling a human-entered value as machine-computed.
            existing.Retire();
            var created = SalaryBand.Create(cmd.GradeId, cmd.Currency, cmd.Midpoint, cmd.PreviousGradeMidpoint, cmd.EffectiveDate, BandProvenance.Designed);
            db.SalaryBands.Add(created);
            bandId = created.Id;
        }

        var rank = await db.Grades.Where(g => g.Id == cmd.GradeId).Select(g => g.Rank).FirstAsync(ct);
        await SalaryBandProjections.CascadeMidpointsAsync(db, rank, cmd.Midpoint, ct);
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new BandPublished(cmd.GradeId, bandId), ct);
        return (await SalaryBandProjections.RowForGradeAsync(db, cmd.GradeId, ct))!;
    }
}
