using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Permissions;

public sealed record DeleteRoleCommand(Guid Id);

// Closed, never deleted, once a user has ever been assigned it (Core Spec §3.4).
internal sealed class DeleteRoleHandler(CoreDbContext db)
    : ICommandHandler<DeleteRoleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteRoleCommand cmd, CancellationToken ct)
    {
        var role = await db.Roles.FindAsync(cmd.Id, ct);
        if (role is null) return Error.NotFound("Role not found.");

        var everAssigned = await db.RoleAssignments.AnyAsync(a => a.RoleId == cmd.Id, ct);
        if (everAssigned)
        {
            role.Close();
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteRoleEndpoint
{
    public static void MapDeleteRoleEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteRoleHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteRoleCommand(id), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
