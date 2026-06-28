using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

public sealed record CreateSalaryBandCommand(Guid GradeId, string Currency, decimal Midpoint, decimal SpreadPct, decimal OverlapPct, DateOnly EffectiveDate);

internal sealed class CreateSalaryBandHandler(JobRewardDbContext db)
    : ICommandHandler<CreateSalaryBandCommand, Result<SalaryBandRowDto>>
{
    public async Task<Result<SalaryBandRowDto>> Handle(CreateSalaryBandCommand cmd, CancellationToken ct)
    {
        if (cmd.Midpoint <= 0) return Error.Validation("Midpoint must be greater than zero.");
        if (string.IsNullOrWhiteSpace(cmd.Currency)) return Error.Validation("Currency is required.");
        if (!await db.Grades.AnyAsync(g => g.Id == cmd.GradeId, ct)) return Error.NotFound("Grade not found.");
        if (await db.SalaryBands.AnyAsync(b => b.GradeId == cmd.GradeId && b.JobFamilyId == null, ct))
            return Error.Conflict("This grade already has a band; update it instead.");

        db.SalaryBands.Add(SalaryBand.Create(
            cmd.GradeId, cmd.Currency, cmd.Midpoint, cmd.SpreadPct, cmd.OverlapPct, cmd.EffectiveDate));
        await db.SaveChangesAsync(ct);
        return (await SalaryBandProjections.RowForGradeAsync(db, cmd.GradeId, ct))!;
    }
}
