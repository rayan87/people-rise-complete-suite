using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Locations;

public sealed record ListLocationsQuery();

internal sealed class ListLocationsHandler(CoreDbContext db)
    : IQueryHandler<ListLocationsQuery, Result<IReadOnlyList<LocationDto>>>
{
    public async Task<Result<IReadOnlyList<LocationDto>>> Handle(ListLocationsQuery query, CancellationToken ct)
    {
        var rows = await db.Locations.OrderBy(l => l.Code)
            .Select(l => new LocationDto(l.Id, l.Code, l.NameEn, l.NameAr, l.City, l.Country))
            .ToListAsync(ct);
        return Result<IReadOnlyList<LocationDto>>.Success(rows);
    }
}

internal static class ListLocationsEndpoint
{
    public static void MapListLocationsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListLocationsHandler h, CancellationToken ct) =>
            (await h.Handle(new ListLocationsQuery(), ct)).ToHttp());
    }
}
