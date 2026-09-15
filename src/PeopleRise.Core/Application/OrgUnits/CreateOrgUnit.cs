using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.OrgUnits;

public sealed record CreateOrgUnitCommand(string Code, string NameEn, string? NameAr, Guid? ParentId, Guid? LocationId);

internal sealed class CreateOrgUnitHandler(CoreDbContext db)
    : ICommandHandler<CreateOrgUnitCommand, Result<OrgUnitDto>>
{
    public async Task<Result<OrgUnitDto>> Handle(CreateOrgUnitCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        if (cmd.ParentId is { } parentId && !await db.OrgUnits.AnyAsync(u => u.Id == parentId, ct))
        {
            return Error.NotFound("Parent org unit not found.");
        }

        if (cmd.LocationId is { } locId && !await db.Locations.AnyAsync(l => l.Id == locId, ct))
        {
            return Error.NotFound("Location not found.");
        }

        // The unit tree has one root (Core Spec §5) - reject a second top-level unit.
        if (cmd.ParentId is null && await db.OrgUnits.AnyAsync(u => u.ParentId == null, ct))
        {
            return Error.Conflict("An org unit tree already has a root; give the new unit a parent.");
        }

        var unit = OrgUnit.Create(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.ParentId, cmd.LocationId);
        db.OrgUnits.Add(unit);
        await db.SaveChangesAsync(ct);
        return new OrgUnitDto(unit.Id, unit.Code, unit.NameEn, unit.NameAr, unit.ParentId, unit.LocationId, null, unit.Status.ToString());
    }
}

internal static class CreateOrgUnitEndpoint
{
    public static void MapCreateOrgUnitEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateOrgUnitCommand cmd, CreateOrgUnitHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());
    }
}
