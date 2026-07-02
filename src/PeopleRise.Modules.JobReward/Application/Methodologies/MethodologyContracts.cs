namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public record MethodologyDto(Guid Id, string Code, string NameEn, string? NameAr, IReadOnlyList<MethodologyVersionDto> Versions);

public record MethodologyVersionDto(Guid Id, int VersionNo, string Status, string? Note, DateTime? PublishedAt);

public record MethodologyVersionDetailDto(
    Guid Id, Guid MethodologyId, string MethodologyCode, string MethodologyNameEn, string? MethodologyNameAr,
    int VersionNo, string Status, string? Note, DateTime? PublishedAt,
    IReadOnlyList<FactorDetailDto> Factors,
    IReadOnlyList<GradeMappingDto> GradeMappings);

public record FactorDto(Guid Id, string Code, string NameEn, string? NameAr, decimal Weight, int SortOrder);
public record FactorDetailDto(Guid Id, string Code, string NameEn, string? NameAr, decimal Weight, int SortOrder, IReadOnlyList<QuestionDetailDto> Questions);

public record QuestionDto(Guid Id, string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, int SortOrder);
public record QuestionDetailDto(Guid Id, string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, int SortOrder, IReadOnlyList<AnswerOptionDto> Options);

public record AnswerOptionDto(Guid Id, string LabelEn, string? LabelAr, int Points, int SortOrder);

public record GradeMappingDto(Guid Id, Guid GradeId, string? GradeCode, int MinScore, int MaxScore);

// Request bodies (the route id is supplied separately by the endpoint). En required, Ar optional.
public record UpdateMethodologyRequest(string NameEn, string? NameAr);
public record CreateMethodologyVersionRequest(string? Note = null);
public record FactorRequest(string Code, string NameEn, string? NameAr, int SortOrder, decimal? Weight = null);
public record QuestionRequest(string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, int SortOrder);
public record AnswerOptionRequest(string LabelEn, string? LabelAr, int Points, int SortOrder);
public record CreateGradeMappingRequest(Guid GradeId, int MinScore, int MaxScore);
public record UpdateGradeMappingRequest(Guid GradeId, int MinScore, int MaxScore);
