using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Positions;

public sealed record UpdatePositionCommand(Guid Id, string Code, Guid OrgUnitId);

internal sealed class UpdatePositionHandler(CoreDbContext db)
    : ICommandHandler<UpdatePositionCommand, Result<PositionDto>>
{
    public async Task<Result<PositionDto>> Handle(UpdatePositionCommand cmd, CancellationToken ct)
    {
        var position = await db.JobPositions.FindAsync([cmd.Id], ct);
        if (position is null)
        {
            return Error.NotFound("Position not found.");
        }

        var orgUnit = await db.OrgUnits.FirstOrDefaultAsync(u => u.Id == cmd.OrgUnitId, ct);
        if (orgUnit is null)
        {
            return Error.NotFound("Org unit not found.");
        }
        if (orgUnit.Status == OrgUnitStatus.Closed)
        {
            return Error.Validation("Cannot move a position onto a closed org unit.");
        }

        try { position.Update(cmd.Code, cmd.OrgUnitId); }
        catch (DomainStateException e) { return Error.Conflict(e.Message); }

        await db.SaveChangesAsync(ct);
        return (await PositionProjections.ByIdAsync(db, position.Id, ct))!;
    }
}

internal static class UpdatePositionEndpoint
{
    public static void MapUpdatePositionEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdatePositionCommand cmd, UpdatePositionHandler h, CancellationToken ct) =>
            (await h.Handle(cmd with { Id = id }, ct)).ToHttp());
    }
}
