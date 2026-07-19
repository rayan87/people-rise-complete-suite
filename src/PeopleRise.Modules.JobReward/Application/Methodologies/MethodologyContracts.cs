namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public record MethodologyDto(Guid Id, string Code, string NameEn, string? NameAr, IReadOnlyList<MethodologyVersionDto> Versions);

public record MethodologyVersionDto(Guid Id, int VersionNo, string Status, string? Note, int MinPoints, int MaxPoints, DateTime? PublishedAt);

public record MethodologyVersionDetailDto(
    Guid Id, Guid MethodologyId, string MethodologyCode, string MethodologyNameEn, string? MethodologyNameAr,
    int VersionNo, string Status, string? Note, int MinPoints, int MaxPoints, DateTime? PublishedAt,
    IReadOnlyList<FactorDetailDto> Factors,
    IReadOnlyList<GradeMappingDto> GradeMappings);

public record FactorDto(Guid Id, string Code, string NameEn, string? NameAr, string? HelpTextEn, string? HelpTextAr, decimal Weight, int SortOrder);
// CalculatedPoints = version.MaxPoints x Factor.Weight / 100 - the factor's point ceiling (an
// allocation, not a scored answer, so left unrounded). Computed once in ScoringService's formula;
// this DTO just re-exposes the same math for the authoring screen, never recomputed client-side.
public record FactorDetailDto(Guid Id, string Code, string NameEn, string? NameAr, string? HelpTextEn, string? HelpTextAr, decimal Weight, int SortOrder, decimal CalculatedPoints, IReadOnlyList<QuestionDetailDto> Questions);

public record QuestionDto(Guid Id, string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, decimal Weight, bool IsRequired, int SortOrder);
// CalculatedPoints = FactorDetailDto.CalculatedPoints x Question.Weight / 100 - same reasoning as above.
public record QuestionDetailDto(Guid Id, string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, decimal Weight, bool IsRequired, int SortOrder, decimal CalculatedPoints, IReadOnlyList<AnswerOptionDto> Options);

// CalculatedPoints = round(QuestionDetailDto.CalculatedPoints x Rating / 5) - rounded, because this is
// exactly what an evaluation answer at this rating would score (mirrors ScoringService.Score).
public record AnswerOptionDto(Guid Id, string LabelEn, string? LabelAr, string? HelpTextEn, string? HelpTextAr, int Rating, int SortOrder, int CalculatedPoints);

public record GradeMappingDto(Guid Id, Guid GradeId, string? GradeCode, int? MinScore, int? MaxScore);

// Request bodies (the route id is supplied separately by the endpoint). En required, Ar optional.
public record UpdateMethodologyRequest(string NameEn, string? NameAr);
public record CreateMethodologyVersionRequest(string? Note = null, int MinPoints = 200, int MaxPoints = 1000);
public record SetPointBudgetRequest(int MinPoints, int MaxPoints);
public record SetGradeRangeRequest(int MinScore, int MaxScore);
