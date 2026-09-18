using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Permissions;

public sealed record CreateRoleCommand(string NameEn, string? NameAr, IReadOnlyList<string> Permissions);

internal sealed class CreateRoleHandler(CoreDbContext db)
    : ICommandHandler<CreateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(CreateRoleCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        if (!TryParsePermissions(cmd.Permissions, out var parsed, out var error))
        {
            return Error.Validation(error!);
        }

        var role = Role.Create(cmd.NameEn, cmd.NameAr, parsed);
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return new RoleDto(role.Id, role.NameEn, role.NameAr, role.Permissions.Select(p => p.ToString()).ToList(), role.Status.ToString());
    }

    internal static bool TryParsePermissions(IReadOnlyList<string> raw, out List<Permission> parsed, out string? error)
    {
        parsed = [];
        foreach (var name in raw)
        {
            if (!Enum.TryParse<Permission>(name, out var permission))
            {
                error = $"Unknown permission '{name}'.";
                return false;
            }
            parsed.Add(permission);
        }
        error = null;
        return true;
    }
}

internal static class CreateRoleEndpoint
{
    public static void MapCreateRoleEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateRoleRequest body, CreateRoleHandler h, CancellationToken ct) =>
            (await h.Handle(new CreateRoleCommand(body.NameEn, body.NameAr, body.Permissions), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
