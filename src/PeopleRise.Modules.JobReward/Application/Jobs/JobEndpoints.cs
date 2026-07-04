using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.Jobs;

internal static class JobEndpoints
{
    public static void MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/jobs");

        group.MapListJobsEndpoint();
        group.MapGetJobEndpoint();
        group.MapCreateJobEndpoint();
        group.MapUpdateJobEndpoint();
        group.MapDeleteJobEndpoint();
    }
}
