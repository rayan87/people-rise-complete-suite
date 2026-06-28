using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Jobs;

public sealed record ListJobsQuery();

internal sealed class ListJobsHandler(JobRewardDbContext db)
    : IQueryHandler<ListJobsQuery, Result<IReadOnlyList<JobDto>>>
{
    public async Task<Result<IReadOnlyList<JobDto>>> Handle(ListJobsQuery query, CancellationToken ct)
    {
        var rows = await db.Jobs.OrderBy(j => j.Code).Select(j => new JobDto(
            j.Id, j.Code, j.TitleEn, j.TitleAr, j.DescriptionEn, j.DescriptionAr,
            j.LevelId, j.Level!.Code, j.Level.NameEn, j.Level.NameAr,
            j.JobFamilyId, j.JobFamily!.Code, j.JobFamily.NameEn, j.JobFamily.NameAr,
            j.GradeId, j.Grade!.Code, j.Grade.NameEn, j.Grade.NameAr,
            j.Status.ToString(),
            db.SalaryBands.Where(b => b.GradeId == j.GradeId && b.JobFamilyId == null)
                .Select(b => new JobBandDto(b.Currency, b.MinAmount, b.Midpoint, b.MaxAmount)).FirstOrDefault()
            )).ToListAsync(ct);
        return Result<IReadOnlyList<JobDto>>.Success(rows);
    }
}
