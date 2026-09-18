using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Permissions;

public sealed record ListRoleAssignmentsQuery(Guid? UserId = null);

internal sealed class ListRoleAssignmentsHandler(CoreDbContext db)
    : IQueryHandler<ListRoleAssignmentsQuery, Result<IReadOnlyList<RoleAssignmentDto>>>
{
    public async Task<Result<IReadOnlyList<RoleAssignmentDto>>> Handle(ListRoleAssignmentsQuery query, CancellationToken ct)
    {
        var q = db.RoleAssignments.AsQueryable();
        if (query.UserId is { } userId) q = q.Where(a => a.UserId == userId);

        var rows = await q.OrderBy(a => a.UserId)
            .Select(a => new RoleAssignmentDto(a.Id, a.UserId, a.RoleId, a.Role!.NameEn))
            .ToListAsync(ct);
        return Result<IReadOnlyList<RoleAssignmentDto>>.Success(rows);
    }
}

internal static class ListRoleAssignmentsEndpoint
{
    public static void MapListRoleAssignmentsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/assignments", async (ListRoleAssignmentsHandler h, CancellationToken ct, Guid? userId = null) =>
            (await h.Handle(new ListRoleAssignmentsQuery(userId), ct)).ToHttp())
            .RequirePermission(Permission.ManagePermissions);
    }
}
