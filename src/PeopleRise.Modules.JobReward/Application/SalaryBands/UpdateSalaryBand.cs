using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

public sealed record UpdateSalaryBandCommand(Guid BandId, string Currency, decimal Midpoint, decimal SpreadPct, decimal OverlapPct, DateOnly EffectiveDate);

internal sealed class UpdateSalaryBandHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateSalaryBandCommand, Result<SalaryBandRowDto>>
{
    public async Task<Result<SalaryBandRowDto>> Handle(UpdateSalaryBandCommand cmd, CancellationToken ct)
    {
        if (cmd.Midpoint <= 0) return Error.Validation("Midpoint must be greater than zero.");
        if (string.IsNullOrWhiteSpace(cmd.Currency)) return Error.Validation("Currency is required.");

        var band = await db.SalaryBands.FirstOrDefaultAsync(b => b.Id == cmd.BandId, ct);
        if (band is null) return Error.NotFound("Salary band not found.");

        band.Update(cmd.Midpoint, cmd.SpreadPct, cmd.OverlapPct, cmd.Currency, cmd.EffectiveDate);
        await db.SaveChangesAsync(ct);
        return (await SalaryBandProjections.RowForGradeAsync(db, band.GradeId, ct))!;
    }
}
