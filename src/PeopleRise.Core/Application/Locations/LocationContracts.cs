namespace PeopleRise.Core.Application.Locations;

public record LocationDto(Guid Id, string Code, string NameEn, string? NameAr, string? City, string? Country);
