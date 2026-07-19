namespace PeopleRise.Modules.JobReward.Application.Methodologies.AnswerOptions;

public record AnswerOptionRequest(string LabelEn, string? LabelAr, string? HelpTextEn, string? HelpTextAr, int Rating, int SortOrder);
