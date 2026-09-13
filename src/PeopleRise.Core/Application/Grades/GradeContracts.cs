namespace PeopleRise.Core.Application.Grades;

public record GradeDto(Guid Id, string Code, string NameEn, string? NameAr, int Rank, Guid LevelId, string? LevelCode);
