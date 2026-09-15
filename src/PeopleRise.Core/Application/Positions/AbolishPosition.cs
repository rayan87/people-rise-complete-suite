using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Positions;

public sealed record AbolishPositionCommand(Guid Id);

// Closed, never deleted, once it has been occupied (Core Spec §7). A never-filled position has
// nothing referencing it and can still be removed outright.
internal sealed class AbolishPositionHandler(CoreDbContext db)
    : ICommandHandler<AbolishPositionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AbolishPositionCommand cmd, CancellationToken ct)
    {
        var position = await db.JobPositions.FindAsync(cmd.Id, ct);
        if (position is null)
        {
            return Error.NotFound("Position not found.");
        }

        var everOccupied = await db.EmployeeAssignments.AnyAsync(a => a.PositionId == cmd.Id, ct);

        if (everOccupied)
        {
            try { position.Abolish(); }
            catch (DomainStateException e) { return Error.Conflict(e.Message); }
        }
        else
        {
            db.JobPositions.Remove(position);
        }

        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class AbolishPositionEndpoint
{
    public static void MapAbolishPositionEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/abolish", async (Guid id, AbolishPositionHandler h, CancellationToken ct) =>
            (await h.Handle(new AbolishPositionCommand(id), ct)).ToHttp());
    }
}
