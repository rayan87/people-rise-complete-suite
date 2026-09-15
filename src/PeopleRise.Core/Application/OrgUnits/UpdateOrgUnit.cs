using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.OrgUnits;

public sealed record UpdateOrgUnitCommand(Guid Id, string Code, string NameEn, string? NameAr, Guid? ParentId, Guid? LocationId);

internal sealed class UpdateOrgUnitHandler(CoreDbContext db)
    : ICommandHandler<UpdateOrgUnitCommand, Result<OrgUnitDto>>
{
    public async Task<Result<OrgUnitDto>> Handle(UpdateOrgUnitCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var unit = await db.OrgUnits.FindAsync([cmd.Id], ct);
        if (unit is null)
        {
            return Error.NotFound("Org unit not found.");
        }

        if (cmd.LocationId is { } locId && !await db.Locations.AnyAsync(l => l.Id == locId, ct))
        {
            return Error.NotFound("Location not found.");
        }

        if (cmd.ParentId != unit.ParentId)
        {
            if (cmd.ParentId == cmd.Id)
            {
                return Error.Validation("An org unit cannot be its own parent.");
            }

            if (cmd.ParentId is { } newParentId)
            {
                if (!await db.OrgUnits.AnyAsync(u => u.Id == newParentId, ct))
                {
                    return Error.NotFound("Parent org unit not found.");
                }

                // No cycles (Core Spec §5): walk the new parent's ancestor chain looking for this unit.
                var ancestry = await db.OrgUnits.Select(u => new { u.Id, u.ParentId }).ToListAsync(ct);
                var byId = ancestry.ToDictionary(u => u.Id, u => u.ParentId);
                var cursor = (Guid?)newParentId;
                while (cursor is { } current)
                {
                    if (current == cmd.Id)
                    {
                        return Error.Validation("This move would create a cycle in the org unit tree.");
                    }
                    byId.TryGetValue(current, out cursor);
                }
            }
            else if (await db.OrgUnits.AnyAsync(u => u.ParentId == null && u.Id != cmd.Id, ct))
            {
                return Error.Conflict("An org unit tree already has a root; this unit cannot become a second one.");
            }
        }

        unit.Update(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.ParentId, cmd.LocationId);
        await db.SaveChangesAsync(ct);
        return new OrgUnitDto(unit.Id, unit.Code, unit.NameEn, unit.NameAr, unit.ParentId, unit.LocationId, null, unit.Status.ToString());
    }
}

internal static class UpdateOrgUnitEndpoint
{
    public static void MapUpdateOrgUnitEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateOrgUnitCommand cmd, UpdateOrgUnitHandler h, CancellationToken ct) =>
            (await h.Handle(cmd with { Id = id }, ct)).ToHttp());
    }
}
