using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Domain;

internal class Methodology : Entity
{
    public string Code { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string? NameAr { get; private set; }

    public ICollection<MethodologyVersion>? Versions { get; set; }

    private Methodology() { }   // EF

    public static Methodology Create(string code, string nameEn, string? nameAr) =>
        new() { Code = code, NameEn = nameEn, NameAr = nameAr };

    public void Update(string nameEn, string? nameAr) { NameEn = nameEn; NameAr = nameAr; }   // a label; editable anytime
}

internal class MethodologyVersion : Entity   // versioning: evaluations pin a version, so re-tuning never re-grades old jobs
{
    public Guid MethodologyId { get; private set; }
    public Methodology? Methodology { get; private set; }
    public int VersionNo { get; private set; }
    public MethodologyVersionStatus Status { get; private set; } = MethodologyVersionStatus.Draft;
    public string? Note { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    private MethodologyVersion() { }   // EF

    public static MethodologyVersion CreateDraft(Guid methodologyId, int versionNo, string? note) =>
        new() { MethodologyId = methodologyId, VersionNo = versionNo, Note = note };

    /// <summary>Guard for every authoring write: a version is editable only while Draft.</summary>
    public void EnsureEditable()
    {
        if (Status != MethodologyVersionStatus.Draft)
            throw new DomainStateException(
                $"Version is {Status}; only a Draft can be edited. Re-tuning publishes a new version.");
    }

    public void Publish(bool hasQuestions, bool hasGradeMappings)
    {
        EnsureEditable();
        if (!hasQuestions) throw new DomainException("Cannot publish: the version has no questions.");
        if (!hasGradeMappings) throw new DomainException("Cannot publish: the version has no grade mappings.");
        Status = MethodologyVersionStatus.Active;
        PublishedAt = DateTime.UtcNow;
    }

    public void Retire() => Status = MethodologyVersionStatus.Retired;
}

internal class Factor : Entity
{
    public Guid MethodologyVersionId { get; private set; }
    public MethodologyVersion? MethodologyVersion { get; private set; }
    public string Code { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string? NameAr { get; private set; }
    public decimal Weight { get; private set; } = 1m;
    public int SortOrder { get; private set; }

    private Factor() { }   // EF

    public static Factor Create(Guid versionId, string code, string nameEn, string? nameAr, decimal weight, int sortOrder) =>
        new() { MethodologyVersionId = versionId, Code = code, NameEn = nameEn, NameAr = nameAr, Weight = weight, SortOrder = sortOrder };

    public void Update(string code, string nameEn, string? nameAr, decimal weight, int sortOrder)
    { Code = code; NameEn = nameEn; NameAr = nameAr; Weight = weight; SortOrder = sortOrder; }
}

internal class Question : Entity
{
    public Guid FactorId { get; private set; }
    public Factor? Factor { get; private set; }
    public string QuestionTextEn { get; private set; } = "";
    public string? QuestionTextAr { get; private set; }
    public string? HelpTextEn { get; private set; }
    public string? HelpTextAr { get; private set; }
    public QuestionType QuestionType { get; private set; } = QuestionType.SingleChoice;
    public int SortOrder { get; private set; }

    private Question() { }   // EF

    public static Question Create(Guid factorId, string questionTextEn, string? questionTextAr,
                                  string? helpTextEn, string? helpTextAr, QuestionType questionType, int sortOrder) =>
        new()
        {
            FactorId = factorId, QuestionTextEn = questionTextEn, QuestionTextAr = questionTextAr,
            HelpTextEn = helpTextEn, HelpTextAr = helpTextAr, QuestionType = questionType, SortOrder = sortOrder,
        };

    public void Update(string questionTextEn, string? questionTextAr, string? helpTextEn, string? helpTextAr, QuestionType questionType, int sortOrder)
    { QuestionTextEn = questionTextEn; QuestionTextAr = questionTextAr; HelpTextEn = helpTextEn; HelpTextAr = helpTextAr; QuestionType = questionType; SortOrder = sortOrder; }
}

internal class AnswerOption : Entity   // simple rule: answer = points
{
    public Guid QuestionId { get; private set; }
    public Question? Question { get; private set; }
    public string LabelEn { get; private set; } = "";
    public string? LabelAr { get; private set; }
    public int Points { get; private set; }
    public int SortOrder { get; private set; }

    private AnswerOption() { }   // EF

    public static AnswerOption Create(Guid questionId, string labelEn, string? labelAr, int points, int sortOrder) =>
        new() { QuestionId = questionId, LabelEn = labelEn, LabelAr = labelAr, Points = points, SortOrder = sortOrder };

    public void Update(string labelEn, string? labelAr, int points, int sortOrder)
    { LabelEn = labelEn; LabelAr = labelAr; Points = points; SortOrder = sortOrder; }
}

internal class GradeMapping : Entity   // score range -> grade, per version
{
    public Guid MethodologyVersionId { get; private set; }
    public MethodologyVersion? MethodologyVersion { get; private set; }
    public Guid GradeId { get; private set; }
    public Grade? Grade { get; private set; }
    public int MinScore { get; private set; }
    public int MaxScore { get; private set; }

    private GradeMapping() { }   // EF

    public static GradeMapping Create(Guid versionId, Guid gradeId, int minScore, int maxScore) =>
        new() { MethodologyVersionId = versionId, GradeId = gradeId, MinScore = minScore, MaxScore = maxScore };

    public void Update(Guid gradeId, int minScore, int maxScore)
    { GradeId = gradeId; MinScore = minScore; MaxScore = maxScore; }
}

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
}

internal class EvaluationAnswer : ImmutableEntity   // the audit trail - insert-only
{
    public Guid EvaluationId { get; private set; }
    public Evaluation? Evaluation { get; private set; }
    public Guid QuestionId { get; private set; }
    public Question? Question { get; private set; }
    public Guid AnswerOptionId { get; private set; }
    public AnswerOption? AnswerOption { get; private set; }
    public int PointsSnapshot { get; private set; }   // points frozen at answering time

    private EvaluationAnswer() { }   // EF

    public static EvaluationAnswer Create(Guid evaluationId, Guid questionId, Guid answerOptionId, int pointsSnapshot) =>
        new()
        {
            EvaluationId = evaluationId, QuestionId = questionId,
            AnswerOptionId = answerOptionId, PointsSnapshot = pointsSnapshot,
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
