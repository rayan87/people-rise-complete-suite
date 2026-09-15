using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.Locations;

internal static class LocationEndpoints
{
    public static void MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/locations");

        group.MapListLocationsEndpoint();
        group.MapCreateLocationEndpoint();
        group.MapUpdateLocationEndpoint();
        group.MapDeleteLocationEndpoint();
    }
}
