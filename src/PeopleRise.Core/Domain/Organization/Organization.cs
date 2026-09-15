using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>Exactly one per tenant (Core Spec §4) - the descriptive record, distinct from the
/// control-plane Tenant (routing/billing: name is a routing label, database, status, type). Nothing
/// inside a tenant reads the control-plane label, and nothing in the control plane reads this
/// record - two homes for one field guarantee drift, and an on-prem tenant is one tenant database
/// plus the application with no control plane to read from at all, so everything the Core UI needs
/// to render an organization's profile must live here. Current-state record: corrections overwrite,
/// no effective dating.</summary>
internal class Organization : Entity
{
    public string LegalNameEn { get; set; } = "";

    public string? LegalNameAr { get; set; }

    public string? TradeNameEn { get; set; }

    public string? TradeNameAr { get; set; }

    // Industry: a controlled vocabulary (seeded IndustryClassification rows), not free text.
    // IndustryCode is the primary code everything downstream keys on; secondaries describe and
    // never drive. "Other" is the escape hatch for a customer who doesn't fit the standard.
    public string? IndustryCode { get; set; }

    public string? IndustryOtherText { get; set; }

    public string? SecondaryIndustryCodes { get; set; }   // comma-delimited IndustryClassification codes

    // Sector determines which legal regime governs the employment relationship (§4: "not cosmetic").
    public OrganizationSector? Sector { get; set; }

    public string BaseCurrency { get; set; } = "";

    public int FiscalYearStartMonth { get; set; } = 1;   // 1-12

    // Comma-delimited System.DayOfWeek names (e.g. "Friday,Saturday") - kept as a delimited string
    // rather than a child table since this is a small, rarely-multi-valued field and the spec is
    // explicitly "no schema" at this level of detail.
    public string? WeekendDays { get; set; }

    // Reduced daily working hours observed during Ramadan (a routine Egyptian labour practice).
    // Null = the organization does not observe a Ramadan reduction.
    public decimal? RamadanDailyHours { get; set; }

    public string? SupportedLanguages { get; set; }   // comma-delimited language codes, e.g. "en,ar"

    public string? DefaultLocale { get; set; }         // e.g. "en-EG"
}

public enum OrganizationSector 
{ 
    Private, 
    StateOwned, 
    Government 
}   // Core Spec §4: "sector is not cosmetic"