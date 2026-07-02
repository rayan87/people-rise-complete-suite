using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record DeleteMethodologyCommand(Guid MethodologyId);

internal sealed class DeleteMethodologyHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteMethodologyCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteMethodologyCommand cmd, CancellationToken ct)
    {
        var methodology = await db.Methodologies
            .Include(m => m.Versions)
            .FirstOrDefaultAsync(x => x.Id == cmd.MethodologyId, ct);

        if (methodology is null)
        {
            return Error.NotFound("Methodology not found.");
        }

        var hasPublished = methodology.Versions!.Any(v =>
            v.Status == MethodologyVersionStatus.Active ||
             v.Status == MethodologyVersionStatus.Retired);

        if (hasPublished)
        {
            return Error.Conflict("Methodology has published versions and cannot be deleted.");
        }
            
        db.Methodologies.Remove(methodology);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
