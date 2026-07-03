using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Application.Methodologies.Factors;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;

public sealed record DeleteGradeMappingCommand(Guid VersionId, Guid MappingId);

internal sealed class DeleteGradeMappingHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteGradeMappingCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteGradeMappingCommand cmd, CancellationToken ct)
    {
        var version = await db.MethodologyVersions
            .Include(version => version.GradeMappings)
            .FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        try 
        { 
            var removed = version.RemoveGradeMapping(cmd.MappingId); 

            if (!removed)
            {
                return Error.NotFound("Grade mapping not found.");
            }
        } 
        catch (DomainStateException e) 
        { 
            return Error.Conflict(e.Message); 
        }
        catch (DomainException e)
        {
            return Error.Validation(e.Message);
        }

        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteGradeMappingEndpoint
{
    public static void MapDeleteGradeMappingEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{versionId:guid}/grade-mappings/{mappingId:guid}",
            async (Guid versionId, Guid mappingId, DeleteGradeMappingHandler h, CancellationToken ct) =>
                (await h.Handle(new DeleteGradeMappingCommand(versionId, mappingId), ct)).ToHttp());
    }
}
