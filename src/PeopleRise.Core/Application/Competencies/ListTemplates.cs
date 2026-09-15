using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Competencies;

public sealed record ListTemplatesQuery();

internal sealed class ListTemplatesHandler(CoreDbContext db)
    : IQueryHandler<ListTemplatesQuery, Result<IReadOnlyList<CompetencyTemplateDto>>>
{
    public async Task<Result<IReadOnlyList<CompetencyTemplateDto>>> Handle(ListTemplatesQuery query, CancellationToken ct)
    {
        var ids = await db.CompetencyTemplates.Select(t => t.Id).ToListAsync(ct);
        var rows = new List<CompetencyTemplateDto>(ids.Count);
        foreach (var id in ids)
        {
            var dto = await TemplateProjections.ByIdAsync(db, id, ct);
            if (dto is not null) rows.Add(dto);
        }
        return Result<IReadOnlyList<CompetencyTemplateDto>>.Success(rows);
    }
}

internal static class ListTemplatesEndpoint
{
    public static void MapListTemplatesEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (ListTemplatesHandler h, CancellationToken ct) =>
            (await h.Handle(new ListTemplatesQuery(), ct)).ToHttp());
    }
}
