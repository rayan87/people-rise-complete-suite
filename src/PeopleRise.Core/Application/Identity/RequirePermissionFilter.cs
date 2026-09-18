using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PeopleRise.Core.Domain;
using PeopleRise.Tenancy;

namespace PeopleRise.Core.Application.Permissions;

/// <summary>Gates one endpoint on the caller holding a specific permission (Core Spec §3.6). Applied
/// today to the Pay endpoints - the one sensitivity class (§3.5) that currently has real data behind
/// it (compa-ratio, held competency profiles, assessment results, and burnout signals don't exist
/// yet). A product may narrow, never widen, what the tenant's role model grants - this filter only
/// narrows.</summary>
internal sealed class RequirePermissionFilter(Permission permission) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (!user.IsAuthenticated)
        {
            return Results.Unauthorized();
        }

        var permissions = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var granted = await permissions.GetGrantedPermissionsAsync(user.UserId, context.HttpContext.RequestAborted);

        if (!granted.Contains(permission.ToString()))
        {
            return Results.Json(new { error = $"Missing permission: {permission}." }, statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}

internal static class PermissionEndpointExtensions
{
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, Permission permission) =>
        builder.AddEndpointFilter(new RequirePermissionFilter(permission));
}
