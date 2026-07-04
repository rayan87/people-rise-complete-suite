using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PeopleRise.Modules.JobReward.Application.Methodologies;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.JobFamilies;

public sealed record CreateJobFamilyCommand(string Code, string NameEn, string? NameAr);

internal sealed class CreateJobFamilyHandler(JobRewardDbContext db)
    : ICommandHandler<CreateJobFamilyCommand, Result<JobFamilyDto>>
{
    public async Task<Result<JobFamilyDto>> Handle(CreateJobFamilyCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn))
        {
            return Error.Validation("English name is required.");
        }

        var family = JobFamily.Create(cmd.Code, cmd.NameEn, cmd.NameAr);
        db.JobFamilies.Add(family);
        await db.SaveChangesAsync(ct);
        return new JobFamilyDto(family.Id, family.Code, family.NameEn, family.NameAr);
    }
}

internal static class CreateJobFamilyEndpoint
{
    public static void MapCreateJobFamilyEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateJobFamilyCommand cmd, CreateJobFamilyHandler h, CancellationToken ct) =>
            (await h.Handle(cmd, ct)).ToHttp());
    }
}
