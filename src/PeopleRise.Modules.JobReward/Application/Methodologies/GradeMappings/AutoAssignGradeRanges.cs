using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;

// Automatic half of the two-step grade flow: tile the version's point budget evenly - no gaps, no
// overlap - across every currently-assigned grade, ordered by the grade's own rank.
public sealed record AutoAssignGradeRangesCommand(Guid VersionId);

internal sealed class AutoAssignGradeRangesHandler(JobRewardDbContext db)
    : ICommandHandler<AutoAssignGradeRangesCommand, Result<IReadOnlyList<GradeMappingDto>>>
{
    public async Task<Result<IReadOnlyList<GradeMappingDto>>> Handle(AutoAssignGradeRangesCommand cmd, CancellationToken ct)
    {
        var version = await db.MethodologyVersions
            .Include(v => v.GradeMappings)
            .FirstOrDefaultAsync(v => v.Id == cmd.VersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        if (version.GradeMappings is null || version.GradeMappings.Count == 0)
        {
            return Error.Validation("The version has no assigned grades to range.");
        }

        var gradeMappingIds = version.GradeMappings.Select(g => g.Id).ToList();
        var rankByMappingId = await db.GradeMappings
            .Where(g => gradeMappingIds.Contains(g.Id))
            .Join(db.Grades, g => g.GradeId, grade => grade.Id, (g, grade) => new { g.Id, grade.Rank })
            .ToDictionaryAsync(x => x.Id, x => x.Rank, ct);

        var orderedIds = gradeMappingIds.OrderBy(id => rankByMappingId[id]).ToList();

        try
        {
            version.AutoAssignGradeRanges(orderedIds);
        }
        catch (DomainStateException e)
        {
            return Error.Conflict(e.Message);
        }
        catch (DomainException e)
        {
            return Error.Validation(e.Message);
        }

        await db.SaveChangesAsync(ct);

        return version.GradeMappings
            .Select(g => new GradeMappingDto(g.Id, g.GradeId, null, g.MinScore, g.MaxScore))
            .ToList();
    }
}

internal static class AutoAssignGradeRangesEndpoint
{
    public static void MapAutoAssignGradeRangesEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{versionId:guid}/grade-mappings/auto-range",
            async (Guid versionId, AutoAssignGradeRangesHandler h, CancellationToken ct) =>
                (await h.Handle(new AutoAssignGradeRangesCommand(versionId), ct)).ToHttp());
    }
}
