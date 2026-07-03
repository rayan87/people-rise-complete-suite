namespace PeopleRise.Modules.JobReward.Application.Methodologies.AnswerOptions;

public record AnswerOptionRequest(string LabelEn, string? LabelAr, int Points, int SortOrder);
