using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

internal class OrgUnit : Entity
{
    public Guid? ParentId { get; private set; }

    public OrgUnit? Parent { get; private set; }

    public string Code { get; private set; } = "";

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public Guid? LocationId { get; private set; }

    public Location? Location { get; private set; }

    public OrgUnitStatus Status { get; private set; } = OrgUnitStatus.Active;

    private OrgUnit() { }   // EF

    public static OrgUnit Create(string code, string nameEn, string? nameAr, Guid? parentId, Guid? locationId)
    {
        return new() 
        { 
            Code = code, 
            NameEn = nameEn, 
            NameAr = nameAr, 
            ParentId = parentId, 
            LocationId = locationId 
        };
    }
    
    public void Update(string code, string nameEn, string? nameAr, Guid? parentId, Guid? locationId)
    { 
        Code = code; 
        NameEn = nameEn; 
        NameAr = nameAr; 
        ParentId = parentId; 
        LocationId = locationId; 
    }

    /// <summary>Closed, never deleted, once anything has referenced it (Core Spec §5).</summary>
    public void Close()
    {
        Status = OrgUnitStatus.Closed;
    }
}

public enum OrgUnitStatus 
{ 
    Active,
    Closed 
}   // Core Spec §5: closed, never deleted, once it has held a position