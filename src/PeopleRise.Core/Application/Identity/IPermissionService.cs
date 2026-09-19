using Microsoft.AspNetCore.Identity;
using PeopleRise.Core.Domain;

namespace PeopleRise.Core.Application.Identity;

/// <summary>Cross-module contract (registered in CoreModule, exactly like IEventPublisher/
/// IEntitlementService) so any module can ask what an Account may do without reading Core's
/// internal tables. <paramref name="accountId"/> is a tenant Account id (Core Spec §11: "the actor
/// is always the account").</summary>
public interface IPermissionService
{
    Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(Guid accountId, CancellationToken ct);
}

internal sealed class PermissionService(UserManager<Account> users, RoleManager<AccountRole> roles) : IPermissionService
{
    public async Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(Guid accountId, CancellationToken ct)
    {
        var account = await users.FindByIdAsync(accountId.ToString());
        if (account is null || account.Status != AccountStatus.Active) return new HashSet<string>();

        var granted = new HashSet<string>();
        foreach (var roleName in await users.GetRolesAsync(account))
        {
            var role = await roles.FindByNameAsync(roleName);
            if (role is null || role.Status != RoleStatus.Active) continue;

            foreach (var claim in await roles.GetClaimsAsync(role))
                if (claim.Type == AccountRole.PermissionClaimType) granted.Add(claim.Value);
        }

        return granted;
    }
}
