using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.CareerPaths;

internal static class CareerPathEndpoints
{
    public static void MapCareerPathEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/career-paths");
        group.MapListCareerPathsEndpoint();
        group.MapCreateCareerPathEndpoint();
        group.MapUpdateCareerPathEndpoint();
        group.MapDeleteCareerPathEndpoint();
    }
}
