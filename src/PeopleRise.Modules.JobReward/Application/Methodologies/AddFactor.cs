using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record AddFactorCommand(Guid VersionId, string Code, string NameEn, string? NameAr, int SortOrder, decimal? Weight);

internal sealed class AddFactorHandler(JobRewardDbContext db)
    : ICommandHandler<AddFactorCommand, Result<FactorDto>>
{
    public async Task<Result<FactorDto>> Handle(AddFactorCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NameEn)) return Error.Validation("English name is required.");

        var v = await db.MethodologyVersions.FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);
        if (v is null) return Error.NotFound("Methodology version not found.");
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        var f = Factor.Create(cmd.VersionId, cmd.Code, cmd.NameEn, cmd.NameAr, cmd.Weight ?? 1m, cmd.SortOrder);
        db.Factors.Add(f);
        await db.SaveChangesAsync(ct);
        return new FactorDto(f.Id, f.Code, f.NameEn, f.NameAr, f.Weight, f.SortOrder);
    }
}
