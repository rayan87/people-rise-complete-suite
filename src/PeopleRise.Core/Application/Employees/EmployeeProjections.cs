using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;

namespace PeopleRise.Core.Application.Employees;

/// <summary>Joins in the employee's CURRENT open assignment (if any) - a separated employee, or one
/// between seats, simply has none (Core Spec §8: the roster points at the position, never the other
/// way, so this is the one place that direction is crossed, and only for display).</summary>
internal static class EmployeeProjections
{
    public static Task<EmployeeDto?> ByIdAsync(CoreDbContext db, Guid id, CancellationToken ct) =>
        Project(db, db.Employees.Where(e => e.Id == id)).FirstOrDefaultAsync(ct);

    public static Task<List<EmployeeDto>> ListAsync(CoreDbContext db, CancellationToken ct) =>
        Project(db, db.Employees.OrderBy(e => e.EmployeeNo)).ToListAsync(ct);

    private static IQueryable<EmployeeDto> Project(CoreDbContext db, IQueryable<Employee> employees) =>
        employees.Select(e => new EmployeeDto(
            e.Id, e.EmployeeNo, e.FullNameEn, e.FullNameAr, e.HireDate, e.EmploymentStatus.ToString(),
            e.PrimaryLocationId, e.PrimaryLocation == null ? null : e.PrimaryLocation.Code,
            db.EmployeeAssignments.Where(a => a.EmployeeId == e.Id && a.EndDate == null)
                .Select(a => (Guid?)a.PositionId).FirstOrDefault(),
            db.EmployeeAssignments.Where(a => a.EmployeeId == e.Id && a.EndDate == null)
                .Select(a => a.Position!.Code).FirstOrDefault(),
            db.EmployeeAssignments.Where(a => a.EmployeeId == e.Id && a.EndDate == null)
                .Select(a => (Guid?)a.Position!.JobId).FirstOrDefault(),
            db.EmployeeAssignments.Where(a => a.EmployeeId == e.Id && a.EndDate == null)
                .Select(a => a.Position!.Job!.TitleEn).FirstOrDefault(),
            db.EmployeeAssignments.Where(a => a.EmployeeId == e.Id && a.EndDate == null)
                .Select(a => a.Position!.OrgUnit!.Code).FirstOrDefault()));
}
