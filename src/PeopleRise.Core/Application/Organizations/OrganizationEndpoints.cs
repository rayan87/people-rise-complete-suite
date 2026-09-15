using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.Organizations;

internal static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/organization");

        group.MapGetOrganizationEndpoint();
        group.MapUpdateOrganizationEndpoint();
    }
}
