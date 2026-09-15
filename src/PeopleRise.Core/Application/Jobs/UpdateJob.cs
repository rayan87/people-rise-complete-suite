using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Events;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Jobs;

public sealed record UpdateJobCommand(
    Guid Id, string Code, string TitleEn, string? TitleAr,
    string? DescriptionEn = null, string? DescriptionAr = null, Guid? JobFamilyId = null);

internal sealed class UpdateJobHandler(CoreDbContext db, IEventPublisher events)
    : ICommandHandler<UpdateJobCommand, Result<JobDto>>
{
    public async Task<Result<JobDto>> Handle(UpdateJobCommand cmd, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([cmd.Id], ct);
        if (job is null) return Error.NotFound("Job not found.");
        if (string.IsNullOrWhiteSpace(cmd.TitleEn)) return Error.Validation("English title is required.");
        if (cmd.JobFamilyId is { } fid && !await db.JobFamilies.AnyAsync(f => f.Id == fid, ct))
            return Error.NotFound("Job family not found.");

        var previousJobFamilyId = job.JobFamilyId;
        job.Update(cmd.Code, cmd.TitleEn, cmd.TitleAr,
                   cmd.DescriptionEn, cmd.DescriptionAr, cmd.JobFamilyId);
        await db.SaveChangesAsync(ct);

        if (previousJobFamilyId != job.JobFamilyId)
            await events.PublishAsync(new JobFamilyChanged(job.Id, job.JobFamilyId), ct);

        // Update never touches grade - fetch it fresh rather than re-deriving here (it's a query,
        // not a column - Core Spec §3.2b).
        return (await JobProjections.ByIdAsync(db, job.Id, ct))!;
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
