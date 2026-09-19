using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>The route a job family takes through a set of grades (Core Spec §5) - a structural
/// declaration about the organization, sitting here because it's true of the organization
/// regardless of which product they bought. Whether a given PERSON qualifies for the next step
/// needs the competency gap and performance history and belongs to Talent Management - not built,
/// out of Phase 1, same split as promotion (the declared route is core, the judgement isn't).
/// A family may have more than one path (e.g. an IC track and a management track), so this binds
/// to JobFamily, not the other way around.</summary>
internal class CareerPath : Entity
{
    public Guid JobFamilyId { get; private set; }

    public JobFamily? JobFamily { get; private set; }

    public string NameEn { get; private set; } = "";

    public string? NameAr { get; private set; }

    private CareerPath() { }   // EF

    public static CareerPath Create(Guid jobFamilyId, string nameEn, string? nameAr) =>
        new() { JobFamilyId = jobFamilyId, NameEn = nameEn, NameAr = nameAr };

    public void Update(string nameEn, string? nameAr)
    {
        NameEn = nameEn;
        NameAr = nameAr;
    }
}

/// <summary>One ordered step (a grade) along a career path.</summary>
internal class CareerPathStep : Entity
{
    public Guid CareerPathId { get; private set; }

    public Guid GradeId { get; private set; }

    public Grade? Grade { get; private set; }

    public int StepOrder { get; private set; }   // 1, 2, 3... the sequence along the path

    private CareerPathStep() { }   // EF

    public static CareerPathStep Create(Guid careerPathId, Guid gradeId, int stepOrder) =>
        new() { CareerPathId = careerPathId, GradeId = gradeId, StepOrder = stepOrder };
}
