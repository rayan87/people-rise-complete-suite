using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Organizations;

public sealed record UpdateOrganizationCommand(
    string LegalNameEn, string? LegalNameAr, string? TradeNameEn, string? TradeNameAr,
    string? IndustryCode, string? IndustryOtherText, string? SecondaryIndustryCodes, string? Sector,
    string BaseCurrency, int FiscalYearStartMonth, string? WeekendDays, decimal? RamadanDailyHours,
    string? SupportedLanguages, string? DefaultLocale);

internal sealed class UpdateOrganizationHandler(CoreDbContext db, IQueryHandler<GetOrganizationQuery, Result<OrganizationDto>> getOrganization)
    : ICommandHandler<UpdateOrganizationCommand, Result<OrganizationDto>>
{
    public async Task<Result<OrganizationDto>> Handle(UpdateOrganizationCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.LegalNameEn))
        {
            return Error.Validation("English legal name is required.");
        }

        if (string.IsNullOrWhiteSpace(cmd.BaseCurrency))
        {
            return Error.Validation("Base currency is required.");
        }

        OrganizationSector? sector = null;
        if (!string.IsNullOrWhiteSpace(cmd.Sector))
        {
            if (!Enum.TryParse<OrganizationSector>(cmd.Sector, out var parsed))
                return Error.Validation($"Sector '{cmd.Sector}' must be Private, StateOwned, or Government.");
            sector = parsed;
        }

        // Industry is a controlled vocabulary (Core Spec §4) - "Other" is the only free-text escape.
        // One primary code, with optional secondaries; both draw from the same seeded list.
        if (!string.IsNullOrWhiteSpace(cmd.IndustryCode)
            && !await db.IndustryClassifications.AnyAsync(i => i.Code == cmd.IndustryCode, ct))
        {
            return Error.Validation($"Unknown industry code '{cmd.IndustryCode}'. Use Other with free text instead.");
        }

        if (!string.IsNullOrWhiteSpace(cmd.SecondaryIndustryCodes))
        {
            var secondaryCodes = cmd.SecondaryIndustryCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var knownCodes = await db.IndustryClassifications
                .Where(i => secondaryCodes.Contains(i.Code)).Select(i => i.Code).ToListAsync(ct);
            var unknown = secondaryCodes.Except(knownCodes).ToList();
            if (unknown.Count > 0)
            {
                return Error.Validation($"Unknown secondary industry code(s): {string.Join(", ", unknown)}.");
            }
        }

        var org = await db.Organizations.FirstOrDefaultAsync(ct);
        if (org is null)
        {
            return Error.NotFound("Organization not provisioned for this tenant.");
        }

        org.LegalNameEn = cmd.LegalNameEn;
        org.LegalNameAr = cmd.LegalNameAr;
        org.TradeNameEn = cmd.TradeNameEn;
        org.TradeNameAr = cmd.TradeNameAr;
        org.IndustryCode = cmd.IndustryCode;
        org.IndustryOtherText = cmd.IndustryOtherText;
        org.SecondaryIndustryCodes = cmd.SecondaryIndustryCodes;
        org.Sector = sector;
        org.BaseCurrency = cmd.BaseCurrency;
        org.FiscalYearStartMonth = cmd.FiscalYearStartMonth;
        org.WeekendDays = cmd.WeekendDays;
        org.RamadanDailyHours = cmd.RamadanDailyHours;
        org.SupportedLanguages = cmd.SupportedLanguages;
        org.DefaultLocale = cmd.DefaultLocale;

        await db.SaveChangesAsync(ct);
        return await getOrganization.Handle(new GetOrganizationQuery(), ct);
    }
}

internal static class UpdateOrganizationEndpoint
{
    public static void MapUpdateOrganizationEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/", async (UpdateOrganizationRequest body, UpdateOrganizationHandler h, CancellationToken ct) =>
            (await h.Handle(new UpdateOrganizationCommand(
                body.LegalNameEn, body.LegalNameAr, body.TradeNameEn, body.TradeNameAr,
                body.IndustryCode, body.IndustryOtherText, body.SecondaryIndustryCodes, body.Sector,
                body.BaseCurrency, body.FiscalYearStartMonth, body.WeekendDays, body.RamadanDailyHours,
                body.SupportedLanguages, body.DefaultLocale), ct)).ToHttp());
    }
}
