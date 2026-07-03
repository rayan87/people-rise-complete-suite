using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class GradeMapping : Entity   // score range -> grade, per version
{
    public Guid MethodologyVersionId { get; private set; }

    public MethodologyVersion? MethodologyVersion { get; private set; }

    public Guid GradeId { get; private set; }

    public Grade? Grade { get; private set; }

    public int MinScore { get; private set; }

    public int MaxScore { get; private set; }

    private GradeMapping() { }   // EF

    public static GradeMapping Create(MethodologyVersion version, Guid gradeId, int minScore, int maxScore)
    {
        ensureValidScore(minScore, maxScore);

        return new()
        {
            MethodologyVersionId = version.Id,
            MethodologyVersion = version,
            GradeId = gradeId,
            MinScore = minScore,
            MaxScore = maxScore
        };
    }

    public void Update(Guid gradeId, int minScore, int maxScore)
    {
        ensureValidScore(minScore, maxScore);

        GradeId = gradeId; 
        MinScore = minScore; 
        MaxScore = maxScore;
    }
    
    private static void ensureValidScore(int minScore, int maxScore)
    {
        if (minScore > maxScore)
        {
            throw new DomainException("minScore cannot be greater than maxScore.");
        }
    }
}
