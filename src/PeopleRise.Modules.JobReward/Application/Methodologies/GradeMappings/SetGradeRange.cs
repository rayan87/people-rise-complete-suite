using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;

// Manual half of the two-step grade flow: set one already-assigned grade's score range by hand.
public sealed record SetGradeRangeCommand(Guid VersionId, Guid MappingId, int MinScore, int MaxScore);

internal sealed class SetGradeRangeHandler(JobRewardDbContext db)
    : ICommandHandler<SetGradeRangeCommand, Result<GradeMappingDto>>
{
    public async Task<Result<GradeMappingDto>> Handle(SetGradeRangeCommand cmd, CancellationToken ct)
    {
        if (cmd.MaxScore < cmd.MinScore)
        {
            return Error.Validation("maxScore must be >= minScore.");
        }

        var version = await db.MethodologyVersions
            .Include(v => v.GradeMappings)
            .FirstOrDefaultAsync(v => v.Id == cmd.VersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        GradeMapping? gradeMapping;

        try
        {
            gradeMapping = version.SetGradeMappingRange(cmd.MappingId, cmd.MinScore, cmd.MaxScore);

            if (gradeMapping is null)
            {
                return Error.NotFound("Grade mapping not found.");
            }
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
        return new GradeMappingDto(gradeMapping.Id,
            gradeMapping.GradeId,
            null,
            gradeMapping.MinScore,
            gradeMapping.MaxScore);
    }
}

internal static class SetGradeRangeEndpoint
{
    public static void MapSetGradeRangeEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{versionId:guid}/grade-mappings/{mappingId:guid}/range",
            async (Guid versionId, Guid mappingId, SetGradeRangeRequest body, SetGradeRangeHandler h, CancellationToken ct) =>
                (await h.Handle(new SetGradeRangeCommand(versionId, mappingId, body.MinScore, body.MaxScore), ct)).ToHttp());
    }
}
