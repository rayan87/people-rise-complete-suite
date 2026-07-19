using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class GradeMapping : Entity   // score range -> grade, per version
{
    public Guid MethodologyVersionId { get; private set; }

    public MethodologyVersion? MethodologyVersion { get; private set; }

    public Guid GradeId { get; private set; }

    public Grade? Grade { get; private set; }

    /// <summary>Null until the score range is set (grades can be assigned to a version before their
    /// range is decided - see MethodologyVersion.AssignGrade / SetGradeMappingRange / AutoAssignGradeRanges).</summary>
    public int? MinScore { get; private set; }

    public int? MaxScore { get; private set; }

    private GradeMapping() { }   // EF

    public static GradeMapping Create(MethodologyVersion version, Guid gradeId, int? minScore, int? maxScore)
    {
        EnsureValidScore(minScore, maxScore);

        return new()
        {
            MethodologyVersionId = version.Id,
            MethodologyVersion = version,
            GradeId = gradeId,
            MinScore = minScore,
            MaxScore = maxScore
        };
    }

    public void Update(Guid gradeId, int? minScore, int? maxScore)
    {
        EnsureValidScore(minScore, maxScore);

        GradeId = gradeId;
        MinScore = minScore;
        MaxScore = maxScore;
    }

    /// <summary>Sets or replaces this grade's score range (the manual half of the two-step
    /// assign-then-range flow; also used internally by MethodologyVersion.AutoAssignGradeRanges).</summary>
    public void SetRange(int minScore, int maxScore)
    {
        EnsureValidScore(minScore, maxScore);

        MinScore = minScore;
        MaxScore = maxScore;
    }

    private static void EnsureValidScore(int? minScore, int? maxScore)
    {
        if (minScore is not null && maxScore is not null && minScore > maxScore)
        {
            throw new DomainException("minScore cannot be greater than maxScore.");
        }
    }
}
