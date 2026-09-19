using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Identity;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

// The one derivation the core computes (Core Spec §10 - the single exception to §12's "no
// derivations", because five products would otherwise each compute it separately: Learning, Talent
// Management, Talent Acquisition, Performance, and career-path qualification). Required minus held,
// computed here and read by all of them - never recomputed privately downstream.
public sealed record GetCompetencyGapQuery(Guid EmployeeId);

internal sealed class GetCompetencyGapHandler(CoreDbContext db)
    : IQueryHandler<GetCompetencyGapQuery, Result<CompetencyGapDto>>
{
    public async Task<Result<CompetencyGapDto>> Handle(GetCompetencyGapQuery query, CancellationToken ct)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == query.EmployeeId, ct))
        {
            return Error.NotFound("Employee not found.");
        }

        // Required profile comes from the employee's CURRENT position's job (Core Spec §8: an
        // employee always occupies a position; §10: required profiles bind to the job).
        var jobId = await db.EmployeeAssignments.Where(a => a.EmployeeId == query.EmployeeId && a.EndDate == null)
            .Select(a => (Guid?)a.Position!.JobId).FirstOrDefaultAsync(ct);
        if (jobId is null)
        {
            return new CompetencyGapDto(query.EmployeeId, null, []);
        }

        var requiredResult = await new GetRequiredProfileHandler(db).Handle(new GetRequiredProfileQuery(jobId.Value), ct);
        if (requiredResult.IsFailure) return requiredResult.Error!;

        var heldResult = await new GetHeldProfileHandler(db).Handle(new GetHeldProfileQuery(query.EmployeeId), ct);
        if (heldResult.IsFailure) return heldResult.Error!;

        var heldByCompetency = heldResult.Value.Items.ToDictionary(i => i.CompetencyId, i => i.Level);

        var items = requiredResult.Value.Items.Select(r =>
        {
            var held = heldByCompetency.TryGetValue(r.CompetencyId, out var level) ? level : (int?)null;
            return new CompetencyGapItemDto(r.CompetencyId, r.CompetencyCode, r.CompetencyNameEn, r.CompetencyNameAr,
                r.RequiredLevel, held, r.RequiredLevel - (held ?? 0));
        }).ToList();

        return new CompetencyGapDto(query.EmployeeId, jobId, items);
    }
}

internal static class GetCompetencyGapEndpoint
{
    public static void MapGetCompetencyGapEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/employees/{employeeId:guid}/competency-gap", async (Guid employeeId, GetCompetencyGapHandler h, CancellationToken ct) =>
            (await h.Handle(new GetCompetencyGapQuery(employeeId), ct)).ToHttp())
            .RequirePermission(Permission.ViewSensitiveData);
    }
}
