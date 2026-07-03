namespace PeopleRise.Modules.JobReward.Application.Methodologies.Questions;

public record QuestionRequest(string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, int SortOrder);