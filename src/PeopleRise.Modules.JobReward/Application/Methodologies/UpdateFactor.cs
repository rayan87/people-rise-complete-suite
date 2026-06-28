using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record UpdateFactorCommand(Guid VersionId, Guid FactorId, string Code, string NameEn, string? NameAr, int SortOrder, decimal? Weight);

internal sealed class UpdateFactorHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateFactorCommand, Result<FactorDto>>
{
    public async Task<Result<FactorDto>> Handle(UpdateFactorCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn)) return Error.Validation("English name is required.");

        var f = await db.Factors.FirstOrDefaultAsync(x => x.Id == cmd.FactorId, ct);
        if (f is null || f.MethodologyVersionId != cmd.VersionId) return Error.NotFound("Factor not found.");

        var v = await db.MethodologyVersions.FirstAsync(x => x.Id == f.MethodologyVersionId, ct);
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        f.Update(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.Weight ?? 1m, cmd.SortOrder);
        await db.SaveChangesAsync(ct);
        return new FactorDto(f.Id, f.Code, f.NameEn, f.NameAr, f.Weight, f.SortOrder);
    }
}
