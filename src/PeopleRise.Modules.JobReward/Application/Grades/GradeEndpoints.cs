using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Modules.JobReward.Application.Grades;

internal static class GradeEndpoints
{
    public static void MapGradeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/grades");

        group.MapListGradesEndpoint();
        group.MapCreateGradeEndpoint();
        group.MapUpdateGradeEndpoint();
        group.MapDeleteGradeEndpoint();
    }
}
