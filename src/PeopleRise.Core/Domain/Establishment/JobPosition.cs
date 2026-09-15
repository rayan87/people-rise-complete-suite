using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

internal class JobPosition : Entity   // a SEAT - the establishment counts these
{
    public Guid JobId { get; private set; }

    public Job? Job { get; private set; }

    public Guid OrgUnitId { get; private set; }

    public OrgUnit? OrgUnit { get; private set; }

    public string Code { get; private set; } = "";

    public PositionStatus Status { get; private set; } = PositionStatus.ApprovedVacant;

    private JobPosition() { }   // EF

    public static JobPosition Create(string code, Guid jobId, Guid orgUnitId)
    {
        return new() 
        { 
            Code = code, 
            JobId = jobId, 
            OrgUnitId = orgUnitId 
        };
    }
        
    /// <summary>Only while not Filled - a filled seat's unit/code changes go through the employee's
    /// own Move, not by editing the position under them.</summary>
    public void Update(string code, Guid orgUnitId)
    {
        if (Status == PositionStatus.Filled)
            throw new DomainStateException("Cannot edit a filled position - move the occupant instead.");
        Code = code;
        OrgUnitId = orgUnitId;
    }

    public void Occupy()
    {
        if (Status != PositionStatus.ApprovedVacant)
            throw new DomainStateException($"Position is {Status}; only an approved-vacant position can be occupied.");
        Status = PositionStatus.Filled;
    }

    /// <summary>The seat frees on a move or separation - occupancy is a property of the seat, not
    /// the person (Core Spec §7), so this never touches the roster.</summary>
    public void Vacate() => Status = PositionStatus.ApprovedVacant;

    /// <summary>Closed, never deleted, once it has been occupied (Core Spec §7).</summary>
    public void Abolish()
    {
        if (Status == PositionStatus.Filled)
            throw new DomainStateException("Cannot abolish a filled position - separate or move the occupant first.");
        Status = PositionStatus.Abolished;
    }
}

public enum PositionStatus 
{ 
    ApprovedVacant, 
    Filled,
    Frozen, 
    Abolished 
}   // ApprovedVacant = the "open box"