namespace PeopleRise.Core.Application.OrgUnits;

public record OrgUnitDto(
    Guid Id, string Code, string NameEn, string? NameAr,
    Guid? ParentId, Guid? LocationId, string? LocationCode, string Status);
