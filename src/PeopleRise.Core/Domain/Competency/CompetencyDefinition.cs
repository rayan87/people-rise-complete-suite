using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>The framework's library entry (Core Spec §10). The 1-5 scale is uniform across every
/// competency - a platform rule, not a per-competency or tenant setting - so there is no "scale"
/// field here at all; every Level below is simply understood as 1-5.</summary>
internal class CompetencyDefinition : Entity
{
    public string Code { get; private set; } = "";

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public string? DescriptionEn { get; private set; }

    public string? DescriptionAr { get; private set; }

    public CompetencyProvenance Provenance { get; private set; }

    public CompetencyStatus Status { get; private set; } = CompetencyStatus.Active;

    private CompetencyDefinition() { }   // EF

    public static CompetencyDefinition Create(string code, string nameEn, string? nameAr,
        string? descriptionEn, string? descriptionAr, CompetencyProvenance provenance)
    {
        return new()
        {
            Code = code,
            NameEn = nameEn,
            NameAr = nameAr,
            DescriptionEn = descriptionEn,
            DescriptionAr = descriptionAr,
            Provenance = provenance,
        };
    }

    // Seeded competencies are fully editable and deletable (Core Spec §10) - unlike the ISIC list,
    // this is a deliberate starting point, not a standard.
    public void Update(string code, string nameEn, string? nameAr, string? descriptionEn, string? descriptionAr)
    {
        Code = code;
        NameEn = nameEn;
        NameAr = nameAr;
        DescriptionEn = descriptionEn;
        DescriptionAr = descriptionAr;
    }

    /// <summary>Closed, never deleted, once referenced by a template item or a profile (Core Spec §3.4).</summary>
    public void Close() => Status = CompetencyStatus.Closed;
}

public enum CompetencyProvenance { Seeded, Granted, TenantAuthored }   // Core Spec §3.1/§10

public enum CompetencyStatus { Active, Closed }
