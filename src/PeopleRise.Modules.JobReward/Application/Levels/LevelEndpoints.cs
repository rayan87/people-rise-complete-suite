using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.Levels;

internal static class LevelEndpoints
{
    public static void MapLevelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/levels");

        group.MapListLevelsEndpoint();
        group.MapCreateLevelEndpoint();
        group.MapUpdateLevelEndpoint();
        group.MapDeleteLevelEndpoint();
    }
}
