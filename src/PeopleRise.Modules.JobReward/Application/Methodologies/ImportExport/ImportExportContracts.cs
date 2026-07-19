namespace PeopleRise.Modules.JobReward.Application.Methodologies.ImportExport;

public sealed record ExportedFile(byte[] Content, string FileName);

// Rows parsed from an uploaded workbook, before DB resolution (e.g. GradeCode -> GradeId).
// Keys/FactorKey/QuestionKey are the workbook's own row numbering, used to join sheets.
internal sealed record ParsedVersionInfoRow(int MinPoints, int MaxPoints);
internal sealed record ParsedFactorRow(int Key, string Code, string NameEn, string? NameAr, decimal Weight, int SortOrder);
internal sealed record ParsedQuestionRow(int Key, int FactorKey, string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, decimal Weight, bool IsRequired, int SortOrder);
internal sealed record ParsedAnswerOptionRow(int QuestionKey, string LabelEn, string? LabelAr, int Rating, int SortOrder);
internal sealed record ParsedGradeMappingRow(string GradeCode, int? MinScore, int? MaxScore);

internal sealed record ParsedMethodologyWorkbook(
    ParsedVersionInfoRow VersionInfo,
    IReadOnlyList<ParsedFactorRow> Factors,
    IReadOnlyList<ParsedQuestionRow> Questions,
    IReadOnlyList<ParsedAnswerOptionRow> AnswerOptions,
    IReadOnlyList<ParsedGradeMappingRow> GradeMappings);
