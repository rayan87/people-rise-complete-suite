namespace PeopleRise.Core.Application.Levels;

public record LevelDto(Guid Id, string Code, string NameEn, string? NameAr, int Rank, string Status);
