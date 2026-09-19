using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PeopleRise.SharedKernel;
using PeopleRise.Tenancy;

namespace PeopleRise.Core.Application.Identity;

// Permission gating is server-side; the UI mirrors it (CLAUDE.md/Core Spec §3.6) - a screen needs
// to know its own caller's grants to decide what to render, without itself needing ManagePermissions.
// UserId here is the tenant Account's id (§11: "the actor is always the account") - see
// RequirePermissionFilter's doc comment for how ICurrentUser.UserId maps onto it today.
public sealed record GetMyPermissionsQuery(Guid UserId);

internal sealed class GetMyPermissionsHandler(IPermissionService permissions)
    : IQueryHandler<GetMyPermissionsQuery, Result<IReadOnlyList<string>>>
{
    public async Task<Result<IReadOnlyList<string>>> Handle(GetMyPermissionsQuery query, CancellationToken ct)
    {
        var granted = await permissions.GetGrantedPermissionsAsync(query.UserId, ct);
        return Result<IReadOnlyList<string>>.Success(granted.ToList());
    }
}

internal static class GetMyPermissionsEndpoint
{
    public static void MapGetMyPermissionsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/me/permissions", async (ICurrentUser user, GetMyPermissionsHandler h, CancellationToken ct) =>
        {
            if (!user.IsAuthenticated) return Results.Unauthorized();
            return (await h.Handle(new GetMyPermissionsQuery(user.UserId), ct)).ToHttp();
        });
    }
}
