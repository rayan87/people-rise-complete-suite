namespace PeopleRise.Modules.JobReward.Application.Methodologies.Factors;

public record FactorRequest(string Code, string NameEn, string? NameAr, string? HelpTextEn, string? HelpTextAr, decimal Weight, int SortOrder);
