using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Permissions;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.PayElementAmounts;

// Current amounts by default; the full effective-dated history (Core Spec §3.2b) with includeHistory.
public sealed record ListPayElementAmountsQuery(Guid EmployeeId, bool IncludeHistory = false);

internal sealed class ListPayElementAmountsHandler(CoreDbContext db)
    : IQueryHandler<ListPayElementAmountsQuery, Result<IReadOnlyList<PayElementAmountDto>>>
{
    public async Task<Result<IReadOnlyList<PayElementAmountDto>>> Handle(ListPayElementAmountsQuery query, CancellationToken ct)
    {
        var q = db.EmployeePayElementAmounts.Where(a => a.EmployeeId == query.EmployeeId);
        if (!query.IncludeHistory) q = q.Where(a => a.EndDate == null);

        var rows = await q.OrderByDescending(a => a.EffectiveDate)
            .Select(a => new PayElementAmountDto(a.Id, a.EmployeeId, a.PayElementId, a.PayElement!.Code, a.PayElement.NameEn,
                a.Amount, a.Currency, a.EffectiveDate, a.EndDate, a.Provenance.ToString()))
            .ToListAsync(ct);
        return Result<IReadOnlyList<PayElementAmountDto>>.Success(rows);
    }
}

internal static class ListPayElementAmountsEndpoint
{
    public static void MapListPayElementAmountsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{employeeId:guid}/pay-element-amounts", async (Guid employeeId, ListPayElementAmountsHandler h, CancellationToken ct, bool includeHistory = false) =>
            (await h.Handle(new ListPayElementAmountsQuery(employeeId, includeHistory), ct)).ToHttp())
            .RequirePermission(Permission.ViewSensitiveData);
    }
}
