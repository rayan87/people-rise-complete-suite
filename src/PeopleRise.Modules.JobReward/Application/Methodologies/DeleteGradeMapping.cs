using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record DeleteGradeMappingCommand(Guid VersionId, Guid MappingId);

internal sealed class DeleteGradeMappingHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteGradeMappingCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteGradeMappingCommand cmd, CancellationToken ct)
    {
        var v = await db.MethodologyVersions.FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);
        if (v is null) return Error.NotFound("Methodology version not found.");
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        var mapping = await db.GradeMappings
            .FirstOrDefaultAsync(m => m.Id == cmd.MappingId && m.MethodologyVersionId == cmd.VersionId, ct);
        if (mapping is null) return Error.NotFound("Grade mapping not found.");

        db.GradeMappings.Remove(mapping);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
