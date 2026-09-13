using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.JobFamilies;

public sealed record ListJobFamiliesQuery();

internal sealed class ListJobFamiliesHandler(CoreDbContext db)
    : IQueryHandler<ListJobFamiliesQuery, Result<IReadOnlyList<JobFamilyDto>>>
{
    public async Task<Result<IReadOnlyList<JobFamilyDto>>> Handle(ListJobFamiliesQuery query, CancellationToken ct)
    {
        var rows = await db.JobFamilies.OrderBy(f => f.Code)
            .Select(f => new JobFamilyDto(f.Id, f.Code, f.NameEn, f.NameAr))
            .ToListAsync(ct);
        return Result<IReadOnlyList<JobFamilyDto>>.Success(rows);
    }
}

internal static class ListJobFamiliesEndpoint
{
    public static void MapListJobFamiliesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListJobFamiliesHandler h, CancellationToken ct) =>
            (await h.Handle(new ListJobFamiliesQuery(), ct)).ToHttp());
    }
}
