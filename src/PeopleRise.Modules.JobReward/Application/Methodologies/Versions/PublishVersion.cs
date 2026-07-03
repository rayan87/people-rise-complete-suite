using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Versions;

public sealed record PublishVersionCommand(Guid VersionId);

internal sealed class PublishVersionHandler(JobRewardDbContext db)
    : ICommandHandler<PublishVersionCommand, Result<MethodologyVersionDto>>
{
    public async Task<Result<MethodologyVersionDto>> Handle(PublishVersionCommand cmd, CancellationToken ct)
    {
        var version = await db.MethodologyVersions
            .Include(methodology => methodology.Factors)
            .Include(methodology => methodology.GradeMappings)
            .FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        try 
        { 
            version.Publish(); 
        }
        catch (DomainStateException e) 
        { 
            return Error.Conflict(e.Message); 
        }
        catch (DomainException e) 
        { 
            return Error.Validation(e.Message); 
        }

        await retireOtherVersions(version.MethodologyId, version.Id, ct);

        await db.SaveChangesAsync(ct);

        return new MethodologyVersionDto(version.Id, 
            version.VersionNo, 
            version.Status.ToString(), 
            version.Note, 
            version.PublishedAt);
    }

    private async Task retireOtherVersions(Guid methodologyId, Guid versionId, CancellationToken ct)
    {
        await db.MethodologyVersions
            .Where(x => x.MethodologyId == methodologyId
                     && x.Status == MethodologyVersionStatus.Active && x.Id != versionId)
            .ForEachAsync(p => p.Retire(), ct);
    }
}

internal static class PublishVersionEndpoint
{
    public static void MapPublishVersionEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/publish", async (Guid id, PublishVersionHandler h, CancellationToken ct) =>
            (await h.Handle(new PublishVersionCommand(id), ct)).ToHttp());
    }
}