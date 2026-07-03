using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class Methodology : Entity
{
    public string Code { get; private set; } = "";

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public ICollection<MethodologyVersion>? Versions { get; set; }

    private Methodology() { }   // EF

    public static Methodology Create(string code, string nameEn, string? nameAr)
    {
        return new() 
        { 
            Code = code,
            NameEn = nameEn, 
            NameAr = nameAr 
        };
    }
        
    public void Update(string nameEn, string? nameAr) 
    { 
        NameEn = nameEn; 
        NameAr = nameAr; 
    }   // a label; editable anytime

    /// <summary>
    /// Ensures that a methodology can be deleted.
    /// </summary>
    /// <exception cref="DomainException"></exception>
    public void EnsureDeletable()
    {
        if (Versions is null)
        {
            throw new DomainException("Versions must be loaded in order to determine whether this methodology can be deleted.");
        }

        if (Versions.Any(v =>
            v.Status == MethodologyVersionStatus.Active ||
             v.Status == MethodologyVersionStatus.Retired))
        {
            throw new DomainStateException("Methodology has published versions and cannot be deleted.");
        }
    }
}
