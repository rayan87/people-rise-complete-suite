using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.SalaryBands;

public sealed record ListSalaryBandsQuery();

internal sealed class ListSalaryBandsHandler(JobRewardDbContext db)
    : IQueryHandler<ListSalaryBandsQuery, Result<IReadOnlyList<SalaryBandRowDto>>>
{
    public async Task<Result<IReadOnlyList<SalaryBandRowDto>>> Handle(ListSalaryBandsQuery query, CancellationToken ct)
    {
        var rows = await SalaryBandProjections.RowsAsync(db, ct);
        return Result<IReadOnlyList<SalaryBandRowDto>>.Success(rows);
    }
}

internal static class ListSalaryBandsEndpoint
{
    public static void MapListSalaryBandsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListSalaryBandsHandler h, CancellationToken ct) =>
            (await h.Handle(new ListSalaryBandsQuery(), ct)).ToHttp());
    }
}