using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.OrgUnits;

public sealed record DeleteOrgUnitCommand(Guid Id);

/// <summary>Closed, never deleted, once it has ever held a position (Core Spec §5) - checked
/// against ANY position ever pointed at this unit, not just currently-approved ones, since the
/// history is what closure protects.</summary>
internal sealed class DeleteOrgUnitHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<DeleteOrgUnitCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteOrgUnitCommand cmd, CancellationToken ct)
    {
        var unit = await db.OrgUnits.FindAsync(cmd.Id, ct);
        if (unit is null)
        {
            return Error.NotFound("Org unit not found.");
        }

        if (await db.OrgUnits.AnyAsync(u => u.ParentId == cmd.Id, ct))
        {
            return Error.Conflict("This org unit has child units — move or close them first.");
        }

        var everHeldAPosition = await db.JobPositions.AnyAsync(p => p.OrgUnitId == cmd.Id, ct);
        if (everHeldAPosition)
        {
            unit.Close();
            await db.SaveChangesAsync(ct);
            await events.PublishAsync(new OrgUnitClosed(unit.Id), ct);
            return Result<bool>.Success(true);
        }

        db.OrgUnits.Remove(unit);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteOrgUnitEndpoint
{
    public static void MapDeleteOrgUnitEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteOrgUnitHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteOrgUnitCommand(id), ct)).ToHttp());
    }
}
