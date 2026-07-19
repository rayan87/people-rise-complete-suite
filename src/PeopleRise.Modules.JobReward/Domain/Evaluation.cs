using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class Evaluation : Entity
{
    public Guid JobId { get; private set; }
    public Job? Job { get; private set; }
    public Guid MethodologyVersionId { get; private set; }
    public MethodologyVersion? MethodologyVersion { get; private set; }
    public Guid? EvaluatorEmployeeId { get; private set; }
    public Employee? EvaluatorEmployee { get; private set; }
    public EvaluationStatus Status { get; private set; } = EvaluationStatus.Draft;
    public int? TotalScore { get; private set; }
    public Guid? RecommendedGradeId { get; private set; }
    public Grade? RecommendedGrade { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByEmployeeId { get; private set; }

    private Evaluation() { }   // EF

    public static Evaluation CreateDraft(Guid jobId, Guid methodologyVersionId, Guid? evaluatorEmployeeId) =>
        new() { JobId = jobId, MethodologyVersionId = methodologyVersionId, EvaluatorEmployeeId = evaluatorEmployeeId };

    /// <summary>Record the server-computed score + recommended grade and move to Submitted.</summary>
    public void Submit(int totalScore, Guid? recommendedGradeId)
    {
        if (Status != EvaluationStatus.Draft)
            throw new DomainStateException(
                $"Evaluation is {Status}; only a Draft can be submitted. Corrections create a NEW evaluation.");
        TotalScore = totalScore;
        RecommendedGradeId = recommendedGradeId;
        Status = EvaluationStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        if (Status != EvaluationStatus.Submitted)
            throw new DomainStateException($"Evaluation is {Status}; only a Submitted evaluation can be approved.");
        Status = EvaluationStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
    }

    /// <summary>A later evaluation for the same job was approved. Kept for history; answers untouched.</summary>
    public void Supersede()
    {
        if (Status != EvaluationStatus.Approved)
            throw new DomainStateException($"Evaluation is {Status}; only an Approved evaluation can be superseded.");
        Status = EvaluationStatus.Superseded;
    }
}

internal class EvaluationAnswer : ImmutableEntity   // the audit trail - insert-only
{
    public Guid EvaluationId { get; private set; }
    public Evaluation? Evaluation { get; private set; }
    public Guid QuestionId { get; private set; }
    public Question? Question { get; private set; }
    public Guid AnswerOptionId { get; private set; }
    public AnswerOption? AnswerOption { get; private set; }
    public int RatingSnapshot { get; private set; }   // the chosen answer's rating (1-5), frozen at answering time

    private EvaluationAnswer() { }   // EF

    public static EvaluationAnswer Create(Guid evaluationId, Guid questionId, Guid answerOptionId, int ratingSnapshot) =>
        new()
        {
            EvaluationId = evaluationId, QuestionId = questionId,
            AnswerOptionId = answerOptionId, RatingSnapshot = ratingSnapshot,
        };
}

internal class EvaluationFactorScore : ImmutableEntity
{
    public Guid EvaluationId { get; private set; }
    public Evaluation? Evaluation { get; private set; }
    public Guid FactorId { get; private set; }
    public Factor? Factor { get; private set; }
    public int Score { get; private set; }

    private EvaluationFactorScore() { }   // EF

    public static EvaluationFactorScore Create(Guid evaluationId, Guid factorId, int score) =>
        new() { EvaluationId = evaluationId, FactorId = factorId, Score = score };
}
