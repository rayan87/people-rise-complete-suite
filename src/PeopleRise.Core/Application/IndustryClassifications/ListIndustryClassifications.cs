using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.IndustryClassifications;

// Read-only reference data (Core Spec §4: seeded, not editable - no CRUD, only a list).
public sealed record ListIndustryClassificationsQuery();

internal sealed class ListIndustryClassificationsHandler(CoreDbContext db)
    : IQueryHandler<ListIndustryClassificationsQuery, Result<IReadOnlyList<IndustryClassificationDto>>>
{
    public async Task<Result<IReadOnlyList<IndustryClassificationDto>>> Handle(ListIndustryClassificationsQuery query, CancellationToken ct)
    {
        var rows = await db.IndustryClassifications.OrderBy(i => i.Code)
            .Select(i => new IndustryClassificationDto(i.Code, i.Level.ToString(), i.ParentCode, i.NameEn, i.NameAr))
            .ToListAsync(ct);
        return Result<IReadOnlyList<IndustryClassificationDto>>.Success(rows);
    }
}

internal static class ListIndustryClassificationsEndpoint
{
    public static void MapListIndustryClassificationsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/industry-classifications", async (ListIndustryClassificationsHandler h, CancellationToken ct) =>
            (await h.Handle(new ListIndustryClassificationsQuery(), ct)).ToHttp());
    }
}
