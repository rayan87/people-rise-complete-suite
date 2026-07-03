using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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

        try 
        {
            methodology.EnsureDeletable();
        } 
        catch (DomainStateException e) 
        { 
            return Error.Conflict(e.Message); 
        }
            
        db.Methodologies.Remove(methodology);
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }
}

internal static class DeleteMethodologyEndpoint
{
    public static void MapDeleteMethodologyEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}",
            async (Guid id, DeleteMethodologyHandler h, CancellationToken ct) =>
                (await h.Handle(new DeleteMethodologyCommand(id), ct)).ToHttp());
    }
}
