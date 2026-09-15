using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>A place the organization sits (Core Spec §5: "org units and the unit tree; locations").
/// Not an independent dimension jobs/positions/people each reference separately - a unit sits
/// somewhere, and an employee's primary location is copied from their unit's location at assignment
/// (Core Spec §8). Current-state record: corrections overwrite.</summary>
internal class Location : Entity
{
    public string Code { get; private set; } = "";

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public string? City { get; private set; }

    public string? Country { get; private set; }

    private Location() { }   // EF

    public static Location Create(string code, string nameEn, string? nameAr, string? city, string? country)
    {
        return new() 
        { 
            Code = code, 
            NameEn = nameEn, 
            NameAr = nameAr, 
            City = city, 
            Country = country 
        };
    }
        

    public void Update(string code, string nameEn, string? nameAr, string? city, string? country)
    { 
        Code = code; 
        NameEn = nameEn; 
        NameAr = nameAr; 
        City = city; 
        Country = country; 
    }
}
