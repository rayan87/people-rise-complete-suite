using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;

namespace PeopleRise.Core.Application.Permissions;

/// <summary>Every product enforces access through this one model (Core Spec §3.6) - JobReward (or
/// any future module) resolves this via DI, exactly like IEventPublisher/IEntitlementService,
/// rather than querying Core's internal Role/RoleAssignment tables directly. Permission names are
/// plain strings at this public boundary (see Domain.Permission for the vocabulary they parse
/// against) - the same "enum crosses as a string" convention AssignJobGradeCommand's
/// GradeAssignmentSource mirror and UpdateOrganizationCommand's Sector already use.</summary>
public interface IPermissionService
{
    Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(Guid userId, CancellationToken ct);
}

internal sealed class PermissionService(CoreDbContext db) : IPermissionService
{
    public async Task<IReadOnlySet<string>> GetGrantedPermissionsAsync(Guid userId, CancellationToken ct)
    {
        var csvRows = await db.RoleAssignments
            .Where(a => a.UserId == userId && a.Role!.Status == RoleStatus.Active)
            .Select(a => a.Role!.PermissionsCsv)
            .ToListAsync(ct);

        return csvRows
            .SelectMany(csv => csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet();
    }
}
