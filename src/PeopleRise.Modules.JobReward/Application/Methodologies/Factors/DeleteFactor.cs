using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Factors;

public sealed record DeleteFactorCommand(Guid VersionId, Guid FactorId);

internal sealed class DeleteFactorHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteFactorCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteFactorCommand cmd, CancellationToken ct)
    {
        var version = await db.MethodologyVersions
            .Include(v => v.Factors)
            .Where(v => v.Id == cmd.VersionId)
            .FirstOrDefaultAsync(ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        try 
        {
            var removed = version.RemoveFactor(cmd.FactorId);

            if (!removed)
            {
                return Error.NotFound("Factor not found.");
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

internal static class DeleteFactorEndpoint
{
    public static void MapDeleteFactorEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{versionId:guid}/factors/{factorId:guid}",
            async (Guid versionId, Guid factorId, DeleteFactorHandler h, CancellationToken ct) =>
                (await h.Handle(new DeleteFactorCommand(versionId, factorId), ct)).ToHttp());
    }
}
