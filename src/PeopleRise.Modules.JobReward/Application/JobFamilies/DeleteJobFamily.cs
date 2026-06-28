using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.JobFamilies;

public sealed record DeleteJobFamilyCommand(Guid Id);

internal sealed class DeleteJobFamilyHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteJobFamilyCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteJobFamilyCommand cmd, CancellationToken ct)
    {
        var family = await db.JobFamilies.FindAsync([cmd.Id], ct);
        if (family is null) return Error.NotFound("Job family not found.");

        var jobCount = await db.Jobs.CountAsync(j => j.JobFamilyId == cmd.Id, ct);
        if (jobCount > 0)
            return Error.Conflict($"Job family is in use by {jobCount} job(s) — reassign them before deleting.");

        db.JobFamilies.Remove(family);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
