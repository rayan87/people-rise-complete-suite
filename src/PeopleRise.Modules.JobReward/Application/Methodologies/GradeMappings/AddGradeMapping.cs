using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;

public sealed record AddGradeMappingCommand(Guid VersionId, Guid GradeId, int MinScore, int MaxScore);

internal sealed class AddGradeMappingHandler(JobRewardDbContext db)
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

        if (cmd.MaxScore < cmd.MinScore)
        {
            return Error.Validation("maxScore must be >= minScore.");
        }

        if (!await db.Grades.AnyAsync(g => g.Id == cmd.GradeId, ct))
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