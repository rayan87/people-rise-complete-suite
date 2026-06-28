using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

// Entities are INTERNAL: other modules physically cannot reference them. The boundary is compiler-enforced.
// Human-facing names are bilingual: *En is required (when the field is required), *Ar is optional.
// Codes stay language-neutral. Entities not yet driven by any handler keep open setters.

internal class OrgUnit : Entity
{
    public Guid? ParentId { get; set; }
    public OrgUnit? Parent { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

internal class Level : Entity   // the five El-Delta levels; C-level has InEvalScope = false
{
    public string Code { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string? NameAr { get; private set; }
    public int Rank { get; private set; }
    public bool InEvalScope { get; private set; } = true;

    private Level() { }   // EF

    public static Level Create(string code, string nameEn, string? nameAr, int rank, bool inEvalScope) =>
        new() { Code = code, NameEn = nameEn, NameAr = nameAr, Rank = rank, InEvalScope = inEvalScope };

    public void Update(string code, string nameEn, string? nameAr, int rank, bool inEvalScope)
    { Code = code; NameEn = nameEn; NameAr = nameAr; Rank = rank; InEvalScope = inEvalScope; }
}

internal class JobFamily : Entity   // horizontal cut; nullable on Job, added in the design phase
{
    public string Code { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string? NameAr { get; private set; }

    private JobFamily() { }   // EF

    public static JobFamily Create(string code, string nameEn, string? nameAr) =>
        new() { Code = code, NameEn = nameEn, NameAr = nameAr };

    public void Update(string code, string nameEn, string? nameAr)
    { Code = code; NameEn = nameEn; NameAr = nameAr; }
}

internal class Grade : Entity
{
    public string Code { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string? NameAr { get; private set; }
    public int Rank { get; private set; }
    public Guid? LevelId { get; private set; }
    public Level? Level { get; private set; }

    private Grade() { }   // EF

    public static Grade Create(string code, string nameEn, string? nameAr, int rank, Guid? levelId) =>
        new() { Code = code, NameEn = nameEn, NameAr = nameAr, Rank = rank, LevelId = levelId };

    public void Update(string code, string nameEn, string? nameAr, int rank, Guid? levelId)
    { Code = code; NameEn = nameEn; NameAr = nameAr; Rank = rank; LevelId = levelId; }
}

internal class Job : Entity   // a role DEFINITION - the thing you evaluate
{
    public string Code { get; private set; } = "";
    public string TitleEn { get; private set; } = "";
    public string? TitleAr { get; private set; }
    public string? DescriptionEn { get; private set; }
    public string? DescriptionAr { get; private set; }
    public Guid LevelId { get; private set; }
    public Level? Level { get; private set; }
    public Guid? JobFamilyId { get; private set; }     // nullable: job works before families exist
    public JobFamily? JobFamily { get; private set; }
    public Guid? GradeId { get; private set; }          // nullable: set once evaluated
    public Grade? Grade { get; private set; }
    public JobStatus Status { get; private set; } = JobStatus.Draft;

    private Job() { }   // EF

    public static Job Create(string code, string titleEn, string? titleAr, Guid levelId,
                             string? descriptionEn, string? descriptionAr, Guid? jobFamilyId) =>
        new()
        {
            Code = code, TitleEn = titleEn, TitleAr = titleAr, LevelId = levelId,
            DescriptionEn = descriptionEn, DescriptionAr = descriptionAr, JobFamilyId = jobFamilyId,
        };

    public void Update(string code, string titleEn, string? titleAr, Guid levelId,
                       string? descriptionEn, string? descriptionAr, Guid? jobFamilyId)
    { Code = code; TitleEn = titleEn; TitleAr = titleAr; LevelId = levelId;
      DescriptionEn = descriptionEn; DescriptionAr = descriptionAr; JobFamilyId = jobFamilyId; }

    /// <summary>An approved evaluation stamps the recommended grade onto the job.</summary>
    public void AssignGrade(Guid gradeId)
    {
        GradeId = gradeId;
        Status = JobStatus.Evaluated;
    }
}

internal class JobPosition : Entity   // a SEAT - the establishment counts these
{
    public Guid JobId { get; set; }
    public Job? Job { get; set; }
    public Guid OrgUnitId { get; set; }
    public OrgUnit? OrgUnit { get; set; }
    public string Code { get; set; } = "";
    public PositionStatus Status { get; set; } = PositionStatus.ApprovedVacant;
}

internal class Employee : Entity   // a PERSON - the one you pay
{
    public string EmployeeNo { get; set; } = "";
    public string FullName { get; set; } = "";
}

internal class EmployeeAssignment : Entity   // who fills which seat over time
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid PositionId { get; set; }
    public JobPosition? Position { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }   // null = current
}
