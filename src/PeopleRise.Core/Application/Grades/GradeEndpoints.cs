using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.Grades;

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
