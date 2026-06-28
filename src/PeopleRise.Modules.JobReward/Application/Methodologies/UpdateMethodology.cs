using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record UpdateMethodologyCommand(Guid Id, string NameEn, string? NameAr);

internal sealed class UpdateMethodologyHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateMethodologyCommand, Result<MethodologyDto>>
{
    public async Task<Result<MethodologyDto>> Handle(UpdateMethodologyCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn)) return Error.Validation("English name is required.");

        var m = await db.Methodologies.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (m is null) return Error.NotFound("Methodology not found.");

        m.Update(cmd.NameEn, cmd.NameAr);
        await db.SaveChangesAsync(ct);
        return new MethodologyDto(m.Id, m.Code, m.NameEn, m.NameAr, []);
    }
}
