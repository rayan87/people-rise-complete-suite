namespace PeopleRise.Modules.JobReward.Application.Levels;

public record LevelDto(Guid Id, string Code, string NameEn, string? NameAr, int Rank, bool InEvalScope);
