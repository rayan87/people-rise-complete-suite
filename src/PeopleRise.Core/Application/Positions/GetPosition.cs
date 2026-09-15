using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Positions;

public sealed record GetPositionQuery(Guid Id);

internal sealed class GetPositionHandler(CoreDbContext db)
    : IQueryHandler<GetPositionQuery, Result<PositionDto>>
{
    public async Task<Result<PositionDto>> Handle(GetPositionQuery query, CancellationToken ct)
    {
        var dto = await PositionProjections.ByIdAsync(db, query.Id, ct);
        return dto is null ? Error.NotFound("Position not found.") : dto;
    }
}

internal static class GetPositionEndpoint
{
    public static void MapGetPositionEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, GetPositionHandler h, CancellationToken ct) =>
            (await h.Handle(new GetPositionQuery(id), ct)).ToHttp());
    }
}
