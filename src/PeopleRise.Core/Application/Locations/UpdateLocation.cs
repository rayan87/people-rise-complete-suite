using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Locations;

public sealed record UpdateLocationCommand(Guid Id, string Code, string NameEn, string? NameAr, string? City, string? Country);

internal sealed class UpdateLocationHandler(CoreDbContext db)
    : ICommandHandler<UpdateLocationCommand, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(UpdateLocationCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var location = await db.Locations.FindAsync([cmd.Id], ct);
        if (location is null)
        {
            return Error.NotFound("Location not found.");
        }

        location.Update(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.City, cmd.Country);
        await db.SaveChangesAsync(ct);
        return new LocationDto(location.Id, location.Code, location.NameEn, location.NameAr, location.City, location.Country);
    }
}

internal static class UpdateLocationEndpoint
{
    public static void MapUpdateLocationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateLocationCommand cmd, UpdateLocationHandler h, CancellationToken ct) =>
            (await h.Handle(cmd with { Id = id }, ct)).ToHttp());
    }
}
