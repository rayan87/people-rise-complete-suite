using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record ListMethodologiesQuery();

internal sealed class ListMethodologiesHandler(JobRewardDbContext db)
    : IQueryHandler<ListMethodologiesQuery, Result<IReadOnlyList<MethodologyDto>>>
{
    public async Task<Result<IReadOnlyList<MethodologyDto>>> Handle(ListMethodologiesQuery query, CancellationToken ct)
    {
        var methodologies = await db.Methodologies
            .Select(m =>
                new MethodologyDto
                (
                    m.Id,
                    m.Code,
                    m.NameEn,
                    m.NameAr,
                    m.Versions!
                        .OrderByDescending(v => v.VersionNo)
                        .Select(v =>
                        new MethodologyVersionDto
                        (
                            v.Id,
                            v.VersionNo,
                            v.Status.ToString(),
                            v.Note,
                            v.PublishedAt
                        ))
                        .ToList()
                )).ToListAsync(ct);
            
        return Result<IReadOnlyList<MethodologyDto>>.Success(methodologies);
    }
}
