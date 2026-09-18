using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Permissions;

public sealed record RevokeRoleCommand(Guid AssignmentId);

// A grant, not an effective-dated or provenance-bearing record - revoking simply removes the row
// (nothing else ever references a RoleAssignment, so there is nothing to close).
internal sealed class RevokeRoleHandler(CoreDbContext db)
    : ICommandHandler<RevokeRoleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RevokeRoleCommand cmd, CancellationToken ct)
    {
        var assignment = await db.RoleAssignments.FindAsync(cmd.AssignmentId, ct);
        if (assignment is null) return Error.NotFound("Role assignment not found.");

        db.RoleAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class RevokeRoleEndpoint
{
    public static void MapRevokeRoleEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/assignments/{assignmentId:guid}", async (Guid assignmentId, RevokeRoleHandler h, CancellationToken ct) =>
            (await h.Handle(new RevokeRoleCommand(assignmentId), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
