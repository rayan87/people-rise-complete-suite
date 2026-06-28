namespace PeopleRise.Modules.JobReward.Application.Evaluations;

public record AnswerSelection(Guid QuestionId, Guid AnswerOptionId);

/// <summary>Request body for submitting answers (the evaluation id comes from the route).</summary>
public record SubmitAnswersRequest(IReadOnlyList<AnswerSelection> Answers);

public record EvaluationCreatedDto(Guid Id, Guid JobId, Guid MethodologyVersionId, string Status);

public record FactorScoreDto(Guid FactorId, string FactorCode, string FactorNameEn, string? FactorNameAr, int Score);

public record AnswerAuditDto(
    Guid QuestionId, string QuestionTextEn, string? QuestionTextAr,
    Guid AnswerOptionId, string AnswerLabelEn, string? AnswerLabelAr, int PointsSnapshot);

public record EvaluationResultDto(
    Guid Id, Guid JobId, string JobCode, string JobTitleEn, string? JobTitleAr,
    Guid MethodologyVersionId, string Status, int? TotalScore,
    Guid? RecommendedGradeId, string? RecommendedGradeCode, string? RecommendedGradeNameEn, string? RecommendedGradeNameAr,
    DateTime? SubmittedAt, DateTime? ApprovedAt,
    IReadOnlyList<FactorScoreDto> FactorScores,
    IReadOnlyList<AnswerAuditDto> Answers);

public record EvaluationListItemDto(
    Guid Id, Guid JobId, string JobCode, string JobTitleEn, string? JobTitleAr,
    Guid MethodologyVersionId, string Status, int? TotalScore,
    Guid? RecommendedGradeId, string? RecommendedGradeCode, DateTime CreatedAt);

// ---- calibration (dry-run, no persistence) ----
public record CalibrationJob(Guid JobId, IReadOnlyList<AnswerSelection> Answers);

public record CalibrationRowDto(
    int Rank, Guid JobId, string JobCode, string JobTitleEn, string? JobTitleAr,
    int TotalScore, Guid? RecommendedGradeId, string? RecommendedGradeCode);

public record CalibrationResultDto(Guid MethodologyVersionId, IReadOnlyList<CalibrationRowDto> Ranking);
