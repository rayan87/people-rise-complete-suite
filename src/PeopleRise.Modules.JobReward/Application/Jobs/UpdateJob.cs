using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Jobs;

public sealed record UpdateJobCommand(
    Guid Id, string Code, string TitleEn, string? TitleAr, Guid LevelId,
    string? DescriptionEn = null, string? DescriptionAr = null, Guid? JobFamilyId = null);

internal sealed class UpdateJobHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(UpdateJobCommand cmd, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([cmd.Id], ct);
        if (job is null) return Error.NotFound("Job not found.");
        if (string.IsNullOrWhiteSpace(cmd.TitleEn)) return Error.Validation("English title is required.");
        if (!await db.Levels.AnyAsync(l => l.Id == cmd.LevelId, ct))
            return Error.NotFound("Level not found.");
        if (cmd.JobFamilyId is { } fid && !await db.JobFamilies.AnyAsync(f => f.Id == fid, ct))
            return Error.NotFound("Job family not found.");

        job.Update(cmd.Code, cmd.TitleEn, cmd.TitleAr, cmd.LevelId,
                   cmd.DescriptionEn, cmd.DescriptionAr, cmd.JobFamilyId);
        await db.SaveChangesAsync(ct);

        return new JobDto(
            job.Id, job.Code, job.TitleEn, job.TitleAr, job.DescriptionEn, job.DescriptionAr,
            job.LevelId, null, null, null, job.JobFamilyId, null, null, null,
            job.GradeId, null, null, null, job.Status.ToString(), job.GradeSource?.ToString(), null);
    }
}

internal static class UpdateJobEndpoint
{
    public static void MapUpdateJobEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", async (Guid id, UpdateJobCommand cmd, UpdateJobHandler h, CancellationToken ct) =>
            (await h.Handle(cmd with { Id = id }, ct)).ToHttp());
    }
}
