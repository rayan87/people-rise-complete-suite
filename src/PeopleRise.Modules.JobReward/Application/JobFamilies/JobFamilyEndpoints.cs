using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.JobFamilies;

internal static class JobFamilyEndpoints
{
    public static void MapJobFamilyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/job-families");

        group.MapListJobFamiliesEndpoint();
        group.MapCreateJobFamilyEndpoint();
        group.MapUpdateJobFamilyEndpoint();
        group.MapDeleteJobFamilyEndpoint();
    }
}
