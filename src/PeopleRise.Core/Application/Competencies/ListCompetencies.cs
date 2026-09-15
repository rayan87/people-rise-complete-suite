using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

public sealed record ListCompetenciesQuery(bool IncludeClosed = false);

internal sealed class ListCompetenciesHandler(CoreDbContext db)
    : IQueryHandler<ListCompetenciesQuery, Result<IReadOnlyList<CompetencyDto>>>
{
    public async Task<Result<IReadOnlyList<CompetencyDto>>> Handle(ListCompetenciesQuery query, CancellationToken ct)
    {
        var q = db.CompetencyDefinitions.AsQueryable();
        if (!query.IncludeClosed) q = q.Where(c => c.Status == CompetencyStatus.Active);

        var rows = await q.OrderBy(c => c.Code)
            .Select(c => new CompetencyDto(c.Id, c.Code, c.NameEn, c.NameAr, c.DescriptionEn, c.DescriptionAr, c.Provenance.ToString(), c.Status.ToString()))
            .ToListAsync(ct);
        return Result<IReadOnlyList<CompetencyDto>>.Success(rows);
    }
}

internal static class ListCompetenciesEndpoint
{
    public static void MapListCompetenciesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListCompetenciesHandler h, CancellationToken ct, bool includeClosed = false) =>
            (await h.Handle(new ListCompetenciesQuery(includeClosed), ct)).ToHttp());
    }
}
