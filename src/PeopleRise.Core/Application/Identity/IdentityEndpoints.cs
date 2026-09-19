using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace PeopleRise.Core.Application.Identity;

internal static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/identity");
        group.MapGetMyPermissionsEndpoint();

        var accounts = group.MapGroup("/accounts");
        accounts.MapCreateAccountEndpoint();
        accounts.MapListAccountsEndpoint();
        accounts.MapGetAccountEndpoint();
        accounts.MapCloseAccountEndpoint();
        accounts.MapListAccountRolesEndpoint();

        var roles = group.MapGroup("/roles");
        roles.MapListRolesEndpoint();
        roles.MapCreateRoleEndpoint();
        roles.MapUpdateRoleEndpoint();
        roles.MapDeleteRoleEndpoint();
        roles.MapAssignRoleEndpoint();
        roles.MapRevokeRoleEndpoint();
    }
}
