using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Versions;

public sealed record CreateVersionCommand(Guid MethodologyId, string? Note);

internal sealed class CreateVersionHandler(JobRewardDbContext db)
    : ICommandHandler<CreateVersionCommand, Result<MethodologyVersionDto>>
{
    public async Task<Result<MethodologyVersionDto>> Handle(CreateVersionCommand cmd, CancellationToken ct)
    {
        if (!await db.Methodologies.AnyAsync(m => m.Id == cmd.MethodologyId, ct))
        {
            return Error.NotFound("Methodology not found.");
        }
            
        var nextVersionNo = await db.MethodologyVersions
            .Where(v => v.MethodologyId == cmd.MethodologyId)
            .Select(v => (int?)v.VersionNo).MaxAsync(ct) ?? 0;

        var version = MethodologyVersion.CreateDraft(cmd.MethodologyId, nextVersionNo + 1, cmd.Note);
        db.MethodologyVersions.Add(version);
        await db.SaveChangesAsync(ct);
        return new MethodologyVersionDto(version.Id, version.VersionNo, version.Status.ToString(), version.Note, null);
    }
}

internal static class CreateVersionEndpoint
{
    public static void MapCreateVersionEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/versions",
            async (Guid id, CreateMethodologyVersionRequest body, CreateVersionHandler h, CancellationToken ct) =>
                (await h.Handle(new CreateVersionCommand(id, body.Note), ct)).ToHttp());
    }
}
