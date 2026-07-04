using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Jobs;

public sealed record CreateJobCommand(
    string Code, string TitleEn, string? TitleAr, Guid LevelId,
    string? DescriptionEn = null, string? DescriptionAr = null, Guid? JobFamilyId = null);

internal sealed class CreateJobHandler(JobRewardDbContext db)
    : ICommandHandler<CreateJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(CreateJobCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.TitleEn))
        {
            return Error.Validation("English title is required.");
        }

        if (!await db.Levels.AnyAsync(l => l.Id == cmd.LevelId, ct))
        {
            return Error.NotFound("Level not found.");
        }
            
        if (cmd.JobFamilyId is { } fid 
            && !await db.JobFamilies.AnyAsync(f => f.Id == fid, ct))
        {
            return Error.NotFound("Job family not found.");
        }
            
        var job = Job.Create(cmd.Code, 
            cmd.TitleEn, 
            cmd.TitleAr, 
            cmd.LevelId, 
            cmd.DescriptionEn, 
            cmd.DescriptionAr, 
            cmd.JobFamilyId);

        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);
        return new JobDto(
            job.Id, job.Code, job.TitleEn, job.TitleAr, job.DescriptionEn, job.DescriptionAr,
            job.LevelId, null, null, null, job.JobFamilyId, null, null, null,
            job.GradeId, null, null, null, job.Status.ToString(), null);
    }
}

internal static class CreateJobEndpoint
{
    public static void MapCreateJobEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateJobCommand cmd, CreateJobHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());
    }
}
