namespace PeopleRise.Modules.JobReward.Application.Methodologies.Factors;

public record FactorRequest(string Code, string NameEn, string? NameAr, int SortOrder, decimal? Weight = null);
