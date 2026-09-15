using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Jobs;

public sealed record GetJobQuery(Guid Id);

internal sealed class GetJobHandler(CoreDbContext db)
    : IQueryHandler<GetJobQuery, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(GetJobQuery query, CancellationToken ct)
    {
        var dto = await JobProjections.ByIdAsync(db, query.Id, ct);
        return dto is null ? Error.NotFound("Job not found.") : dto;
    }
}

internal static class GetJobEndpoint
{
    public static void MapGetJobEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (Guid id, GetJobHandler h, CancellationToken ct) =>
            (await h.Handle(new GetJobQuery(id), ct)).ToHttp());
    }
}
