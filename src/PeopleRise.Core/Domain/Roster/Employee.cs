using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

internal class Employee : Entity   // a PERSON - the one you pay
{
    public string EmployeeNo { get; private set; } = "";

    public string FullNameEn { get; private set; } = "";

    public string? FullNameAr { get; private set; }

    public DateOnly HireDate { get; private set; }

    public EmploymentStatus EmploymentStatus { get; private set; } = EmploymentStatus.Active;

    public Guid? PrimaryLocationId { get; private set; }   // copied from the unit at assignment, editable per employee - never linked

    public Location? PrimaryLocation { get; private set; }

    private Employee() { }   // EF

    public static Employee Create(string employeeNo, string fullNameEn, string? fullNameAr, DateOnly hireDate, Guid? primaryLocationId)
    {
        return new() 
        { 
            EmployeeNo = employeeNo, 
            FullNameEn = fullNameEn, 
            FullNameAr = fullNameAr, 
            HireDate = hireDate, 
            PrimaryLocationId = primaryLocationId 
        };
    }
        
    public void SetPrimaryLocation(Guid? locationId)
    {
        PrimaryLocationId = locationId;
    }

    /// <summary>Active <-> OnLongTermAbsence only - Separated has position side-effects and only
    /// happens through Separate() below.</summary>
    public void SetEmploymentStatus(EmploymentStatus status)
    {
        if (status == EmploymentStatus.Separated || EmploymentStatus == EmploymentStatus.Separated)
        {
            throw new DomainStateException("Use Separate to end employment - it has position side-effects this transition must not have.");
        }   

        EmploymentStatus = status;
    }

    /// <summary>A separated employee is retained, not removed (Core Spec §8) - there is no delete
    /// path for Employee anywhere in the application layer.</summary>
    public void Separate()
    {
        EmploymentStatus = EmploymentStatus.Separated;
    }
}

public enum EmploymentStatus 
{ 
    Active, 
    OnLongTermAbsence, 
    Separated 
}   // Core Spec §8: structural, not procedural
