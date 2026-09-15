using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.Positions;

internal static class PositionEndpoints
{
    public static void MapPositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/positions");

        group.MapListPositionsEndpoint();
        group.MapGetPositionEndpoint();
        group.MapCreatePositionEndpoint();
        group.MapUpdatePositionEndpoint();
        group.MapAbolishPositionEndpoint();
    }
}
