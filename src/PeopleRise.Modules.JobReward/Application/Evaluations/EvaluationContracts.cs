namespace PeopleRise.Modules.JobReward.Application.Evaluations;

public record AnswerSelection(Guid QuestionId, IReadOnlyList<Guid> AnswerOptionIds);

/// <summary>Request body for submitting answers (the evaluation id comes from the route).</summary>
public record SubmitAnswersRequest(IReadOnlyList<AnswerSelection> Answers);

public record EvaluationCreatedDto(Guid Id, Guid JobId, Guid MethodologyVersionId, string Status);

public record FactorScoreDto(Guid FactorId, string FactorCode, string FactorNameEn, string? FactorNameAr, int Score);

// Points = round(version.MaxPoints x Factor.Weight / 100 x Question.Weight / 100 x RatingSnapshot / 5) -
// mirrors ScoringService.Score's per-question formula exactly, computed fresh here (not stored) so it
// always reflects the pinned methodology version's weights, same pattern as the authoring DTOs.
public record AnswerAuditDto(
    Guid QuestionId, string QuestionTextEn, string? QuestionTextAr,
    Guid AnswerOptionId, string AnswerLabelEn, string? AnswerLabelAr, int RatingSnapshot, int Points,
    Guid FactorId, string FactorCode, string FactorNameEn, string? FactorNameAr);

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
