using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record UpdateGradeMappingCommand(Guid VersionId, Guid MappingId, Guid GradeId, int MinScore, int MaxScore);

internal sealed class UpdateGradeMappingHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateGradeMappingCommand, Result<GradeMappingDto>>
{
    public async Task<Result<GradeMappingDto>> Handle(UpdateGradeMappingCommand cmd, CancellationToken ct)
    {
        var v = await db.MethodologyVersions.FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);
        if (v is null) return Error.NotFound("Methodology version not found.");
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        if (cmd.MaxScore < cmd.MinScore) return Error.Validation("maxScore must be >= minScore.");
        if (!await db.Grades.AnyAsync(g => g.Id == cmd.GradeId, ct)) return Error.NotFound("Grade not found.");

        var mapping = await db.GradeMappings
            .FirstOrDefaultAsync(m => m.Id == cmd.MappingId && m.MethodologyVersionId == cmd.VersionId, ct);
        if (mapping is null) return Error.NotFound("Grade mapping not found.");

        mapping.Update(cmd.GradeId, cmd.MinScore, cmd.MaxScore);
        await db.SaveChangesAsync(ct);
        return new GradeMappingDto(mapping.Id, mapping.GradeId, null, mapping.MinScore, mapping.MaxScore);
    }
}
