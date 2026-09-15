using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Domain;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Jobs;

public sealed record CreateJobCommand(
    string Code, string TitleEn, string? TitleAr,
    string? DescriptionEn = null, string? DescriptionAr = null, Guid? JobFamilyId = null);

internal sealed class CreateJobHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<CreateJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(CreateJobCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.TitleEn))
        {
            return Error.Validation("English title is required.");
        }

        if (cmd.JobFamilyId is { } fid
            && !await db.JobFamilies.AnyAsync(f => f.Id == fid, ct))
        {
            return Error.NotFound("Job family not found.");
        }

        var job = Job.Create(cmd.Code,
            cmd.TitleEn,
            cmd.TitleAr,
            cmd.DescriptionEn,
            cmd.DescriptionAr,
            cmd.JobFamilyId);

        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);
        await events.PublishAsync(new JobCreated(job.Id, job.Code), ct);
        // A freshly created job has no grade assignment yet.
        return new JobDto(
            job.Id, job.Code, job.TitleEn, job.TitleAr, job.DescriptionEn, job.DescriptionAr,
            job.JobFamilyId, null, null, null,
            null, null, null, null,
            null, null, null, null, job.Status.ToString(), null, null);
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
