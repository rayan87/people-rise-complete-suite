using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;

namespace PeopleRise.Core.Application.Positions;

internal static class PositionProjections
{
    public static Task<PositionDto?> ByIdAsync(CoreDbContext db, Guid id, CancellationToken ct) =>
        db.JobPositions.Where(p => p.Id == id).Select(Project(db)).FirstOrDefaultAsync(ct);

    public static Task<List<PositionDto>> ListAsync(CoreDbContext db, CancellationToken ct) =>
        db.JobPositions.OrderBy(p => p.Code).Select(Project(db)).ToListAsync(ct);

    // The job's current grade is a query against JobGradeAssignment, not a column (Core Spec
    // §3.2b/§6) - db is captured here so the correlated subquery can join it.
    private static Expression<Func<JobPosition, PositionDto>> Project(CoreDbContext db) => p =>
        new PositionDto(p.Id, p.Code, p.JobId, p.Job!.Code, p.Job.TitleEn, p.Job.TitleAr,
            db.JobGradeAssignments.Where(a => a.JobId == p.JobId && a.EndDate == null)
                .Select(a => (Guid?)a.GradeId).FirstOrDefault(),
            db.JobGradeAssignments.Where(a => a.JobId == p.JobId && a.EndDate == null)
                .Select(a => a.Grade!.Code).FirstOrDefault(),
            p.OrgUnitId, p.OrgUnit!.Code, p.Status.ToString());
}
