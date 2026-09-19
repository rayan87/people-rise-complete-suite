using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Domain;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Identity;

public sealed record CreateRoleCommand(string Name, string? NameAr, IReadOnlyList<string> Permissions);

internal sealed class CreateRoleHandler(RoleManager<AccountRole> roles)
    : ICommandHandler<CreateRoleCommand, Result<RoleDto>>
{
    public async Task<Result<RoleDto>> Handle(CreateRoleCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Name))
        {
            return Error.Validation("Name is required.");
        }

        if (!TryParsePermissions(cmd.Permissions, out var parsed, out var error))
        {
            return Error.Validation(error!);
        }

        var role = AccountRole.Create(cmd.Name, cmd.NameAr);
        var createResult = await roles.CreateAsync(role);
        if (!createResult.Succeeded)
        {
            return Error.Validation(string.Join(' ', createResult.Errors.Select(e => e.Description)));
        }

        foreach (var permission in parsed)
            await roles.AddClaimAsync(role, new System.Security.Claims.Claim(AccountRole.PermissionClaimType, permission.ToString()));

        return await role.ToDtoAsync(roles);
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
            (await h.Handle(new CreateRoleCommand(body.Name, body.NameAr, body.Permissions), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
