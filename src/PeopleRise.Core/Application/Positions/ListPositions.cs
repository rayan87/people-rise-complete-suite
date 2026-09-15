using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Positions;

public sealed record ListPositionsQuery();

internal sealed class ListPositionsHandler(CoreDbContext db)
    : IQueryHandler<ListPositionsQuery, Result<IReadOnlyList<PositionDto>>>
{
    public async Task<Result<IReadOnlyList<PositionDto>>> Handle(ListPositionsQuery query, CancellationToken ct)
    {
        var rows = await PositionProjections.ListAsync(db, ct);
        return Result<IReadOnlyList<PositionDto>>.Success(rows);
    }
}

internal static class ListPositionsEndpoint
{
    public static void MapListPositionsEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListPositionsHandler h, CancellationToken ct) =>
            (await h.Handle(new ListPositionsQuery(), ct)).ToHttp());
    }
}
