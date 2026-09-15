using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Locations;

public sealed record DeleteLocationCommand(Guid Id);

internal sealed class DeleteLocationHandler(CoreDbContext db)
    : ICommandHandler<DeleteLocationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteLocationCommand cmd, CancellationToken ct)
    {
        var location = await db.Locations.FindAsync(cmd.Id, ct);
        if (location is null)
        {
            return Error.NotFound("Location not found.");
        }

        var orgUnitCount = await db.OrgUnits.CountAsync(u => u.LocationId == cmd.Id, ct);
        var employeeCount = await db.Employees.CountAsync(e => e.PrimaryLocationId == cmd.Id, ct);

        if (orgUnitCount > 0 || employeeCount > 0)
        {
            var parts = new List<string>();
            if (orgUnitCount > 0) parts.Add($"{orgUnitCount} org unit(s)");
            if (employeeCount > 0) parts.Add($"{employeeCount} employee(s)");
            return Error.Conflict($"Location is in use by {string.Join(", ", parts)} — reassign them before deleting.");
        }

        db.Locations.Remove(location);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteLocationEndpoint
{
    public static void MapDeleteLocationEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteLocationHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteLocationCommand(id), ct)).ToHttp());
    }
}
