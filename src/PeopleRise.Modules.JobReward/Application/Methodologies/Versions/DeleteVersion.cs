using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Versions;

public sealed record DeleteVersionCommand(Guid VersionId);

internal sealed class DeleteVersionHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteVersionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteVersionCommand cmd, CancellationToken ct)
    {
        var version = await db.MethodologyVersions
            .FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        try 
        { 
            version.EnsureEditable(); 
        } 
        catch (DomainStateException e) 
        { 
            return Error.Conflict(e.Message); 
        }

        db.MethodologyVersions.Remove(version);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteVersionEndpoint
{
    public static void MapDeleteVersionEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteVersionHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteVersionCommand(id), ct)).ToHttp());
    }
}
