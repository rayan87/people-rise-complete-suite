using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Organizations;

// Exactly one per tenant (Core Spec §4) - no id needed, there is only ever one row.
public sealed record GetOrganizationQuery();

internal sealed class GetOrganizationHandler(CoreDbContext db)
    : IQueryHandler<GetOrganizationQuery, Result<OrganizationDto>>
{
    public async Task<Result<OrganizationDto>> Handle(GetOrganizationQuery query, CancellationToken ct)
    {
        var dto = await db.Organizations.Select(o => new OrganizationDto(
            o.Id, o.LegalNameEn, o.LegalNameAr, o.TradeNameEn, o.TradeNameAr,
            o.IndustryCode,
            db.IndustryClassifications.Where(i => i.Code == o.IndustryCode).Select(i => i.NameEn).FirstOrDefault(),
            db.IndustryClassifications.Where(i => i.Code == o.IndustryCode).Select(i => i.NameAr).FirstOrDefault(),
            o.IndustryOtherText, o.SecondaryIndustryCodes,
            o.Sector == null ? null : o.Sector.ToString(),
            o.BaseCurrency, o.FiscalYearStartMonth, o.WeekendDays, o.RamadanDailyHours,
            o.SupportedLanguages, o.DefaultLocale)).FirstOrDefaultAsync(ct);

        return dto is null ? Error.NotFound("Organization not provisioned for this tenant.") : dto;
    }
}

internal static class GetOrganizationEndpoint
{
    public static void MapGetOrganizationEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (GetOrganizationHandler h, CancellationToken ct) =>
            (await h.Handle(new GetOrganizationQuery(), ct)).ToHttp());
    }
}
