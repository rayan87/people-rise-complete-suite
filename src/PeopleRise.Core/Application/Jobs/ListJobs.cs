using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Jobs;

public sealed record ListJobsQuery(bool IncludeClosed = false);

internal sealed class ListJobsHandler(CoreDbContext db)
    : IQueryHandler<ListJobsQuery, Result<IReadOnlyList<JobDto>>>
{
    public async Task<Result<IReadOnlyList<JobDto>>> Handle(ListJobsQuery query, CancellationToken ct)
    {
        var rows = await JobProjections.ListAsync(db, query.IncludeClosed, ct);
        return Result<IReadOnlyList<JobDto>>.Success(rows);
    }
}

internal static class ListJobsEndpoint
{
    public static void MapListJobsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListJobsHandler h, CancellationToken ct, bool includeClosed = false) =>
            (await h.Handle(new ListJobsQuery(includeClosed), ct)).ToHttp());
    }
}
