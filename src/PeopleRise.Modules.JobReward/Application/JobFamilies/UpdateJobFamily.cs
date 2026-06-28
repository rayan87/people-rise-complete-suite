using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.JobFamilies;

public sealed record UpdateJobFamilyCommand(Guid Id, string Code, string NameEn, string? NameAr);

internal sealed class UpdateJobFamilyHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateJobFamilyCommand, Result<JobFamilyDto>>
{
    public async Task<Result<JobFamilyDto>> Handle(UpdateJobFamilyCommand cmd, CancellationToken ct)
    {
        var family = await db.JobFamilies.FindAsync([cmd.Id], ct);
        if (family is null) return Error.NotFound("Job family not found.");
        if (string.IsNullOrWhiteSpace(cmd.NameEn)) return Error.Validation("English name is required.");

        family.Update(cmd.Code, cmd.NameEn, cmd.NameAr);
        await db.SaveChangesAsync(ct);
        return new JobFamilyDto(family.Id, family.Code, family.NameEn, family.NameAr);
    }
}
