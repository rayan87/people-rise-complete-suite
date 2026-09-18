using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Permissions;

public sealed record AssignRoleCommand(Guid UserId, Guid RoleId);

internal sealed class AssignRoleHandler(CoreDbContext db)
    : ICommandHandler<AssignRoleCommand, Result<RoleAssignmentDto>>
{
    public async Task<Result<RoleAssignmentDto>> Handle(AssignRoleCommand cmd, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == cmd.RoleId, ct);
        if (role is null) return Error.NotFound("Role not found.");

        if (await db.RoleAssignments.AnyAsync(a => a.UserId == cmd.UserId && a.RoleId == cmd.RoleId, ct))
        {
            return Error.Conflict("This user already has this role.");
        }

        var assignment = RoleAssignment.Create(cmd.UserId, cmd.RoleId);
        db.RoleAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);
        return new RoleAssignmentDto(assignment.Id, assignment.UserId, assignment.RoleId, role.NameEn);
    }
}

internal static class AssignRoleEndpoint
{
    public static void MapAssignRoleEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/assignments", async (AssignRoleRequest body, AssignRoleHandler h, CancellationToken ct) =>
            (await h.Handle(new AssignRoleCommand(body.UserId, body.RoleId), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
