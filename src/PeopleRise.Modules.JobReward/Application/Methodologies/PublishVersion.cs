using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record PublishVersionCommand(Guid VersionId);

internal sealed class PublishVersionHandler(JobRewardDbContext db)
    : ICommandHandler<PublishVersionCommand, Result<MethodologyVersionDto>>
{
    public async Task<Result<MethodologyVersionDto>> Handle(PublishVersionCommand cmd, CancellationToken ct)
    {
        var v = await db.MethodologyVersions.FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);
        if (v is null) return Error.NotFound("Methodology version not found.");

        // Sibling-table facts the aggregate can't see — query here, pass in, keep the domain DB-free.
        var hasQuestions = await db.Questions
            .AnyAsync(q => db.Factors.Any(f => f.Id == q.FactorId && f.MethodologyVersionId == v.Id), ct);
        var hasGradeMappings = await db.GradeMappings.AnyAsync(gm => gm.MethodologyVersionId == v.Id, ct);

        try { v.Publish(hasQuestions, hasGradeMappings); }
        catch (DomainStateException e) { return Error.Conflict(e.Message); }
        catch (DomainException e) { return Error.Validation(e.Message); }

        await db.MethodologyVersions
            .Where(x => x.MethodologyId == v.MethodologyId
                     && x.Status == MethodologyVersionStatus.Active && x.Id != v.Id)
            .ForEachAsync(p => p.Retire(), ct);

        await db.SaveChangesAsync(ct);
        return new MethodologyVersionDto(v.Id, v.VersionNo, v.Status.ToString(), v.Note, v.PublishedAt);
    }
}
