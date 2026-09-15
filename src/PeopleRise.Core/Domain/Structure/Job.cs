using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

internal class Job : Entity   // a role DEFINITION - the thing you evaluate
{
    public string Code { get; private set; } = "";

    public string TitleEn { get; private set; } = "";

    public string? TitleAr { get; private set; }

    public string? DescriptionEn { get; private set; }

    public string? DescriptionAr { get; private set; }

    public Guid? JobFamilyId { get; private set; }     // nullable: job works before families exist

    public JobFamily? JobFamily { get; private set; }

    // Grade is NOT a column here - it's effective-dated (Core Spec §3.2b/§6), so the current grade
    // is a query against JobGradeAssignment (see JobProjections/GradeAssignmentWriter), the same
    // pattern EmployeeAssignment established for Roster.

    public JobStatus Status { get; private set; } = JobStatus.Draft;

    private Job() { }   // EF

    public static Job Create(string code, string titleEn, string? titleAr,
                             string? descriptionEn, string? descriptionAr, Guid? jobFamilyId)
    {
        return new()
        {
            Code = code,
            TitleEn = titleEn,
            TitleAr = titleAr,
            DescriptionEn = descriptionEn,
            DescriptionAr = descriptionAr,
            JobFamilyId = jobFamilyId,
        };
    }

    public void Update(string code, string titleEn, string? titleAr,
                       string? descriptionEn, string? descriptionAr, Guid? jobFamilyId)
    {
        Code = code;
        TitleEn = titleEn;
        TitleAr = titleAr;
        DescriptionEn = descriptionEn;
        DescriptionAr = descriptionAr;
        JobFamilyId = jobFamilyId;
    }

    /// <summary>Called by GradeAssignmentWriter whenever a JobGradeAssignment is written for this
    /// job - only the job's own Draft -> Active transition lives here now; the grade itself lives in
    /// that history table.</summary>
    public void OnGraded()
    {
        if (Status == JobStatus.Archived)
            throw new DomainStateException("Cannot grade an archived job.");
        if (Status == JobStatus.Draft)
            Status = JobStatus.Active;
    }

    /// <summary>Closed, never deleted, once anything has referenced it (Core Spec §3.4/§5 - a job
    /// that's been evaluated is the spec's own example). Grade history is preserved.</summary>
    public void Archive()
    {
        Status = JobStatus.Archived;
    }
}

public enum JobStatus
{
    Draft,
    Active,
    Archived
}
