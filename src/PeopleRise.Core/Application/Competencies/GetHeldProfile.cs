using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Permissions;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

// Held competency profiles are in the ONE sensitivity class (Core Spec §3.5) alongside pay amounts,
// compa-ratio, assessment results, and burnout signals - gated the same way as Pay.
public sealed record GetHeldProfileQuery(Guid EmployeeId);

internal sealed class GetHeldProfileHandler(CoreDbContext db)
    : IQueryHandler<GetHeldProfileQuery, Result<HeldProfileDto>>
{
    public async Task<Result<HeldProfileDto>> Handle(GetHeldProfileQuery query, CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == query.EmployeeId, ct))
        {
            return Error.NotFound("Employee not found.");
        }

        var rows = await db.HeldCompetencyProfiles.Where(p => p.EmployeeId == query.EmployeeId)
            .Select(p => new { p.CompetencyId, p.Level, p.Source, p.CreatedAt })
            .ToListAsync(ct);

        // Resolved by precedence, then recency within a source (Core Spec §10) - not pure
        // most-recent-wins, which would let a lenient self-declaration overwrite a rigorous assessment.
        var resolved = rows
            .GroupBy(r => r.CompetencyId)
            .Select(g => g.OrderBy(r => HeldProfileProjections.SourcePrecedence(r.Source)).ThenByDescending(r => r.CreatedAt).First())
            .ToList();

        var competencyIds = resolved.Select(r => r.CompetencyId).ToList();
        var competencies = await db.CompetencyDefinitions.Where(c => competencyIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);

        var items = resolved.Select(r => new HeldCompetencyItemDto(
            r.CompetencyId, competencies[r.CompetencyId].Code, competencies[r.CompetencyId].NameEn, competencies[r.CompetencyId].NameAr,
            r.Level, r.Source.ToString())).ToList();

        return new HeldProfileDto(query.EmployeeId, items);
    }
}

internal static class GetHeldProfileEndpoint
{
    public static void MapGetHeldProfileEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/employees/{employeeId:guid}/held-profile", async (Guid employeeId, GetHeldProfileHandler h, CancellationToken ct) =>
            (await h.Handle(new GetHeldProfileQuery(employeeId), ct)).ToHttp())
            .RequirePermission(Permission.ViewSensitiveData);
    }
}
