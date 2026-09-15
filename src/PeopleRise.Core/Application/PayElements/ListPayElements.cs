using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.PayElements;

public sealed record ListPayElementsQuery();

internal sealed class ListPayElementsHandler(CoreDbContext db)
    : IQueryHandler<ListPayElementsQuery, Result<IReadOnlyList<PayElementDto>>>
{
    public async Task<Result<IReadOnlyList<PayElementDto>>> Handle(ListPayElementsQuery query, CancellationToken ct)
    {
        var rows = await db.PayElements.OrderBy(e => e.Code)
            .Select(e => new PayElementDto(e.Id, e.Code, e.NameEn, e.NameAr, e.Basis.ToString()))
            .ToListAsync(ct);
        return Result<IReadOnlyList<PayElementDto>>.Success(rows);
    }
}

internal static class ListPayElementsEndpoint
{
    public static void MapListPayElementsEndpoint(this RouteGroupBuilder group)
    {
        // The element catalogue is structural metadata (which elements exist), not an amount - the
        // sensitive class (Core Spec §3.5) is pay AMOUNTS and salary bands, not the catalogue itself.
        group.MapGet("/", async (ListPayElementsHandler h, CancellationToken ct) =>
            (await h.Handle(new ListPayElementsQuery(), ct)).ToHttp());
    }
}
