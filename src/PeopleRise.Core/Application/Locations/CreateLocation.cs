using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Locations;

public sealed record CreateLocationCommand(string Code, string NameEn, string? NameAr, string? City, string? Country);

internal sealed class CreateLocationHandler(CoreDbContext db)
    : ICommandHandler<CreateLocationCommand, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(CreateLocationCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var location = Location.Create(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.City, cmd.Country);
        db.Locations.Add(location);
        await db.SaveChangesAsync(ct);
        return new LocationDto(location.Id, location.Code, location.NameEn, location.NameAr, location.City, location.Country);
    }
}

internal static class CreateLocationEndpoint
{
    public static void MapCreateLocationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateLocationCommand cmd, CreateLocationHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());
    }
}
