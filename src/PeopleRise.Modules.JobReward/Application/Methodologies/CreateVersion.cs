using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record CreateVersionCommand(Guid MethodologyId, string? Note);

internal sealed class CreateVersionHandler(JobRewardDbContext db)
    : ICommandHandler<CreateVersionCommand, Result<MethodologyVersionDto>>
{
    public async Task<Result<MethodologyVersionDto>> Handle(CreateVersionCommand cmd, CancellationToken ct)
    {
        if (!await db.Methodologies.AnyAsync(m => m.Id == cmd.MethodologyId, ct))
            return Error.NotFound("Methodology not found.");

        var nextNo = await db.MethodologyVersions.Where(v => v.MethodologyId == cmd.MethodologyId)
            .Select(v => (int?)v.VersionNo).MaxAsync(ct) ?? 0;

        var version = MethodologyVersion.CreateDraft(cmd.MethodologyId, nextNo + 1, cmd.Note);
        db.MethodologyVersions.Add(version);
        await db.SaveChangesAsync(ct);
        return new MethodologyVersionDto(version.Id, version.VersionNo, version.Status.ToString(), version.Note, null);
    }
}
