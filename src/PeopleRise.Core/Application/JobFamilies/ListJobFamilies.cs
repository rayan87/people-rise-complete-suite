using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.JobFamilies;

public sealed record ListJobFamiliesQuery(bool IncludeClosed = false);

internal sealed class ListJobFamiliesHandler(CoreDbContext db)
    : IQueryHandler<ListJobFamiliesQuery, Result<IReadOnlyList<JobFamilyDto>>>
{
    public async Task<Result<IReadOnlyList<JobFamilyDto>>> Handle(ListJobFamiliesQuery query, CancellationToken ct)
    {
        var q = db.JobFamilies.AsQueryable();
        if (!query.IncludeClosed) q = q.Where(f => f.Status == JobFamilyStatus.Active);

        var rows = await q.OrderBy(f => f.Code)
            .Select(f => new JobFamilyDto(f.Id, f.Code, f.NameEn, f.NameAr, f.Status.ToString()))
            .ToListAsync(ct);
        return Result<IReadOnlyList<JobFamilyDto>>.Success(rows);
    }
}

internal static class ListJobFamiliesEndpoint
{
    public static void MapListJobFamiliesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListJobFamiliesHandler h, CancellationToken ct, bool includeClosed = false) =>
            (await h.Handle(new ListJobFamiliesQuery(includeClosed), ct)).ToHttp());
    }
}
