using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.Permissions;

internal static class PermissionEndpoints
{
    public static void MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/permissions");

        group.MapGetMyPermissionsEndpoint();
        group.MapListRolesEndpoint();
        group.MapCreateRoleEndpoint();
        group.MapUpdateRoleEndpoint();
        group.MapDeleteRoleEndpoint();
        group.MapAssignRoleEndpoint();
        group.MapRevokeRoleEndpoint();
        group.MapListRoleAssignmentsEndpoint();
    }
}
