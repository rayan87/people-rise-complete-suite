using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>The effective-dated history of which grade a job has held (Core Spec §3.2b/§6: "each
/// carries a validity window; current value is a query rather than a column"). Job itself stores no
/// GradeId column - every reader queries the open row (EndDate == null) and joins through here, the
/// same pattern EmployeeAssignment already established for Roster (Core Spec §8). Writes only ever
/// go through GradeAssignmentWriter, which enforces "no overlapping windows".</summary>
internal class JobGradeAssignment : Entity
{
    public Guid JobId { get; private set; }

    public Job? Job { get; private set; }

    public Guid GradeId { get; private set; }

    public Grade? Grade { get; private set; }

    public GradeSource Source { get; private set; }

    public DateOnly EffectiveDate { get; private set; }

    public DateOnly? EndDate { get; private set; }   // null = current

    private JobGradeAssignment() { }   // EF

    public static JobGradeAssignment Create(Guid jobId, Guid gradeId, GradeSource source, DateOnly effectiveDate)
    {
        return new() { JobId = jobId, GradeId = gradeId, Source = source, EffectiveDate = effectiveDate };
    }

    public void End(DateOnly endDate)
    {
        EndDate = endDate;
    }
}
