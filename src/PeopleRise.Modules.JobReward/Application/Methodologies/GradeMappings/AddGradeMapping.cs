using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Application.Grades;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;

public sealed record AddGradeMappingCommand(Guid VersionId, Guid GradeId, int? MinScore, int? MaxScore);

internal sealed class AddGradeMappingHandler(
    JobRewardDbContext db, IQueryHandler<ListGradesQuery, Result<IReadOnlyList<GradeDto>>> listGrades)
    : ICommandHandler<AddGradeMappingCommand, Result<GradeMappingDto>>
{
    public async Task<Result<GradeMappingDto>> Handle(AddGradeMappingCommand cmd, CancellationToken ct)
    {
        var version = await db.MethodologyVersions
            .FirstOrDefaultAsync(x => x.Id == cmd.VersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        if (cmd.MinScore is not null && cmd.MaxScore is not null && cmd.MaxScore < cmd.MinScore)
        {
            return Error.Validation("maxScore must be >= minScore.");
        }

        // Grade lives in PeopleRise.Core.
        var gradesResult = await listGrades.Handle(new ListGradesQuery(), ct);
        if (gradesResult.IsFailure) return gradesResult.Error!;
        if (!gradesResult.Value.Any(g => g.Id == cmd.GradeId))
        {
            return Error.NotFound("Grade not found.");
        }

        GradeMapping? gradeMapping = null;

        try
        {
            gradeMapping = version.AddGradeMapping(cmd.GradeId, cmd.MinScore, cmd.MaxScore);
        }
        catch (DomainStateException e)
        {
            return Error.Conflict(e.Message);
        }
        catch (DomainException e)
        {
            return Error.Validation(e.Message);
        }

        db.GradeMappings.Add(gradeMapping);
        await db.SaveChangesAsync(ct);

        return new GradeMappingDto(gradeMapping.Id,
            gradeMapping.GradeId,
            null,
            gradeMapping.MinScore,
            gradeMapping.MaxScore);
    }
}

internal static class AddGradeMappingEndpoint
{
    public static void MapAddGradeMappingEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/grade-mappings",
            async (Guid id, GradeMappingRequest body, AddGradeMappingHandler h, CancellationToken ct) =>
                (await h.Handle(new AddGradeMappingCommand(id, body.GradeId, body.MinScore, body.MaxScore), ct)).ToHttp());
    }
}
