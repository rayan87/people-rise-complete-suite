namespace PeopleRise.Core.Application.Positions;

public record PositionDto(
    Guid Id, string Code, Guid JobId, string JobCode, string JobTitleEn, string? JobTitleAr,
    Guid? GradeId, string? GradeCode, Guid OrgUnitId, string OrgUnitCode, string Status);
