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
