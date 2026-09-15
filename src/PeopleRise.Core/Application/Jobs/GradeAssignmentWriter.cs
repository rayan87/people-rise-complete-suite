using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Jobs;

/// <summary>The one place a job's grade assignment is actually written (Core Spec §6/§3.2b:
/// effective-dated, backdating permitted, overlapping windows forbidden). Shared by the request-time
/// AssignJobGradeHandler and the El-Delta demo seed's evaluation-stamping phase (CoreModule.
/// AssignJobGradesAsync), which has no request scope to resolve the handler from. Does not call
/// SaveChanges - the caller controls the transaction boundary.</summary>
internal static class GradeAssignmentWriter
{
    public static async Task<Result<JobGradeAssignment>> AssignAsync(
        CoreDbContext db, Job job, Guid gradeId, GradeSource source, DateOnly effectiveDate, CancellationToken ct)
    {
        try { job.OnGraded(); }
        catch (DomainStateException e) { return Error.Conflict(e.Message); }

        var current = await db.JobGradeAssignments
            .Where(a => a.JobId == job.Id && a.EndDate == null)
            .FirstOrDefaultAsync(ct);

        if (current is not null && effectiveDate <= current.EffectiveDate)
        {
            return Error.Validation(
                "The new effective date must be after the job's current grade assignment - overlapping windows aren't permitted.");
        }

        current?.End(effectiveDate);

        var assignment = JobGradeAssignment.Create(job.Id, gradeId, source, effectiveDate);
        db.JobGradeAssignments.Add(assignment);
        return Result<JobGradeAssignment>.Success(assignment);
    }
}
