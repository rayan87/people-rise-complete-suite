using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>A row of the ISIC Rev. 4 reference list, cut at section and division level (Core Spec
/// §4). Seeded into every tenant at provisioning, shipped in the provisioning path exactly like the
/// competency seed - a shared international standard, not practice content, so it lives here rather
/// than in the control-plane library. Seeded rows are ours and are NOT editable or deletable: no
/// CRUD is exposed for this entity, only a read list. A customer who doesn't fit selects "Other" on
/// Organization and supplies free text; they do not add a row here.</summary>
internal class IndustryClassification : Entity
{
    public string Code { get; private set; } = "";

    public IsicLevel Level { get; private set; }

    public string? ParentCode { get; private set; }   // set on a Division, pointing at its Section

    public string NameEn { get; private set; } = "";

    public string NameAr { get; private set; } = "";

    private IndustryClassification() { }   // EF

    public static IndustryClassification Create(string code, IsicLevel level, string? parentCode, string nameEn, string nameAr)
    {
        return new() 
        { 
            Code = code, 
            Level = level, 
            ParentCode = parentCode, 
            NameEn = nameEn, 
            NameAr = nameAr 
        };
    }   
}

public enum IsicLevel 
{ 
    Section, 
    Division 
}   // Core Spec §4: ISIC Rev. 4 cut at section + division level