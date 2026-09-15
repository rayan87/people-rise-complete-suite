using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.OrgUnits;

internal static class OrgUnitEndpoints
{
    public static void MapOrgUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/org-units");

        group.MapListOrgUnitsEndpoint();
        group.MapCreateOrgUnitEndpoint();
        group.MapUpdateOrgUnitEndpoint();
        group.MapDeleteOrgUnitEndpoint();
    }
}
