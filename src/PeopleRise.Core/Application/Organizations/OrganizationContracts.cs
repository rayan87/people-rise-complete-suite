namespace PeopleRise.Core.Application.Organizations;

public record OrganizationDto(
    Guid Id, string LegalNameEn, string? LegalNameAr, string? TradeNameEn, string? TradeNameAr,
    string? IndustryCode, string? IndustryNameEn, string? IndustryNameAr, string? IndustryOtherText,
    string? SecondaryIndustryCodes, string? Sector, string BaseCurrency, int FiscalYearStartMonth,
    string? WeekendDays, decimal? RamadanDailyHours, string? SupportedLanguages, string? DefaultLocale);

public record UpdateOrganizationRequest(
    string LegalNameEn, string? LegalNameAr, string? TradeNameEn, string? TradeNameAr,
    string? IndustryCode, string? IndustryOtherText, string? SecondaryIndustryCodes, string? Sector,
    string BaseCurrency, int FiscalYearStartMonth, string? WeekendDays, decimal? RamadanDailyHours,
    string? SupportedLanguages, string? DefaultLocale);
