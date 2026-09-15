using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

internal class EmployeeAssignment : Entity   // who fills which seat over time
{
    public Guid EmployeeId { get; private set; }

    public Employee? Employee { get; private set; }

    public Guid PositionId { get; private set; }

    public JobPosition? Position { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }   // null = current

    private EmployeeAssignment() { }   // EF

    public static EmployeeAssignment Create(Guid employeeId, Guid positionId, DateOnly startDate)
    {
        return new() { EmployeeId = employeeId, PositionId = positionId, StartDate = startDate };
    }

    /// <summary>Effective-dated and never overwritten (Core Spec §8) - ending is the only mutation.</summary>
    public void End(DateOnly endDate)
    {
        EndDate = endDate;
    }
}
