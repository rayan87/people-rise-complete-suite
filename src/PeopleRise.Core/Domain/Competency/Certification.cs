using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>Certifications are live, not dormant (Core Spec §10): nothing is mandatory organization-
/// wide, but specific jobs can require specific certifications, and recording one writes a held
/// competency level with provenance CertificationDerived (see RecordCertification). Insert-only -
/// a certification is a documentary fact, corrected by recording a new one, not editing history.</summary>
internal class Certification : ImmutableEntity
{
    public Guid EmployeeId { get; private set; }

    public Guid CompetencyId { get; private set; }

    public CompetencyDefinition? Competency { get; private set; }

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    public int Level { get; private set; }   // 1-5, the held level this certification proves

    public DateOnly IssuedDate { get; private set; }

    private Certification() { }   // EF

    public static Certification Create(Guid employeeId, Guid competencyId, string nameEn, string? nameAr, int level, DateOnly issuedDate)
    {
        return new()
        {
            EmployeeId = employeeId,
            CompetencyId = competencyId,
            NameEn = nameEn,
            NameAr = nameAr,
            Level = level,
            IssuedDate = issuedDate,
        };
    }
}
