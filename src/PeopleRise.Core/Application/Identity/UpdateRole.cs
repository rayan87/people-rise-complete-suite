using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Permissions;

public sealed record UpdateRoleCommand(Guid Id, string NameEn, string? NameAr, IReadOnlyList<string> Permissions);

internal sealed class UpdateRoleHandler(CoreDbContext db)
    : ICommandHandler<UpdateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand cmd, CancellationToken ct)
    {
        var role = await db.Roles.FindAsync([cmd.Id], ct);
        if (role is null) return Error.NotFound("Role not found.");
        if (string.IsNullOrWhiteSpace(cmd.NameEn)) return Error.Validation("English name is required.");

        if (!CreateRoleHandler.TryParsePermissions(cmd.Permissions, out var parsed, out var error))
        {
            return Error.Validation(error!);
        }

        role.Update(cmd.NameEn, cmd.NameAr, parsed);
        await db.SaveChangesAsync(ct);
        return new RoleDto(role.Id, role.NameEn, role.NameAr, role.Permissions.Select(p => p.ToString()).ToList(), role.Status.ToString());
    }
}

internal static class UpdateRoleEndpoint
{
    public static void MapUpdateRoleEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateRoleRequest body, UpdateRoleHandler h, CancellationToken ct) =>
            (await h.Handle(new UpdateRoleCommand(id, body.NameEn, body.NameAr, body.Permissions), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
