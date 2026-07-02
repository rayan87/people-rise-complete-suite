using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record DeleteVersionCommand(Guid VersionId);

internal sealed class DeleteVersionHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteVersionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteVersionCommand cmd, CancellationToken ct)
    {
        var v = await db.MethodologyVersions.FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);
        if (v is null) return Error.NotFound("Methodology version not found.");
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        db.MethodologyVersions.Remove(v);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
