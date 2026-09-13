using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Domain;

/// <summary>Exactly one per tenant (Core Spec §4) - the descriptive record, distinct from the
/// control-plane Tenant (routing/billing). No CRUD endpoints yet; the shape exists so provisioning
/// has somewhere to put it. Current-state record: corrections overwrite, no effective dating.</summary>
internal class Organization : Entity
{
    public string LegalNameEn { get; set; } = "";
    public string? LegalNameAr { get; set; }
    public string? TradeNameEn { get; set; }
    public string? TradeNameAr { get; set; }
    public string? IndustryCode { get; set; }   // ISIC Rev. 4, section/division - not modeled yet
    public string? Sector { get; set; }         // private | state-owned | government
    public string BaseCurrency { get; set; } = "";
    public int FiscalYearStartMonth { get; set; } = 1;   // 1-12
}
