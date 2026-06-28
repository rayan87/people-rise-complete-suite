using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record DeleteFactorCommand(Guid VersionId, Guid FactorId);

internal sealed class DeleteFactorHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteFactorCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteFactorCommand cmd, CancellationToken ct)
    {
        var v = await db.MethodologyVersions.FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);
        if (v is null) return Error.NotFound("Methodology version not found.");
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        var factor = await db.Factors
            .FirstOrDefaultAsync(f => f.Id == cmd.FactorId && f.MethodologyVersionId == cmd.VersionId, ct);
        if (factor is null) return Error.NotFound("Factor not found.");

        db.Factors.Remove(factor);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
