using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record AddGradeMappingCommand(Guid VersionId, Guid GradeId, int MinScore, int MaxScore);

internal sealed class AddGradeMappingHandler(JobRewardDbContext db)
    : ICommandHandler<AddGradeMappingCommand, Result<GradeMappingDto>>
{
    public async Task<Result<GradeMappingDto>> Handle(AddGradeMappingCommand cmd, CancellationToken ct)
    {
        var v = await db.MethodologyVersions.FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);
        if (v is null) return Error.NotFound("Methodology version not found.");
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        if (cmd.MaxScore < cmd.MinScore) return Error.Validation("maxScore must be >= minScore.");
        if (!await db.Grades.AnyAsync(g => g.Id == cmd.GradeId, ct)) return Error.NotFound("Grade not found.");

        var gm = GradeMapping.Create(cmd.VersionId, cmd.GradeId, cmd.MinScore, cmd.MaxScore);
        db.GradeMappings.Add(gm);
        await db.SaveChangesAsync(ct);
        return new GradeMappingDto(gm.Id, gm.GradeId, null, gm.MinScore, gm.MaxScore);
    }
}
