namespace PeopleRise.Core.Application.PayElements;

public record PayElementDto(Guid Id, string Code, string NameEn, string? NameAr, string Basis);
