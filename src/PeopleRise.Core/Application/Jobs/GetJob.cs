using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Jobs;

public sealed record GetJobQuery(Guid Id);

internal sealed class GetJobHandler(CoreDbContext db)
    : IQueryHandler<GetJobQuery, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(GetJobQuery query, CancellationToken ct)
    {
        var dto = await db.Jobs.Where(j => j.Id == query.Id).Select(j => new JobDto(
            j.Id, j.Code, j.TitleEn, j.TitleAr, j.DescriptionEn, j.DescriptionAr,
            j.JobFamilyId, j.JobFamily!.Code, j.JobFamily.NameEn, j.JobFamily.NameAr,
            j.GradeId, j.Grade!.Code, j.Grade.NameEn, j.Grade.NameAr,
            j.Grade!.LevelId, j.Grade.Level!.Code, j.Grade.Level.NameEn, j.Grade.Level.NameAr,
            j.Status.ToString(),
            j.GradeSource == null ? null : j.GradeSource.ToString()
            )).FirstOrDefaultAsync(ct);
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
