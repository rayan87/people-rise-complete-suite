using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;

public sealed record UpdateGradeMappingCommand(Guid VersionId, Guid MappingId, Guid GradeId, int MinScore, int MaxScore);

internal sealed class UpdateGradeMappingHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateGradeMappingCommand, Result<GradeMappingDto>>
{
    public async Task<Result<GradeMappingDto>> Handle(UpdateGradeMappingCommand cmd, CancellationToken ct)
    {
        var version = await db.MethodologyVersions
            .Include(v => v.GradeMappings)
            .FirstOrDefaultAsync(v => v.Id == cmd.VersionId, ct);

        if (version is null)
        {
            return Error.NotFound("Methodology version not found.");
        }

        var gradeExists = await db.Grades.AnyAsync(grade => grade.Id == cmd.GradeId);

        if (!gradeExists)
        {
            return Error.NotFound("Grade not found.");
        }

        GradeMapping? gradeMapping;

        try 
        { 
            gradeMapping = version.UpdateGradeMapping(cmd.MappingId, cmd.GradeId, cmd.MinScore, cmd.MaxScore); 

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

internal static class UpdateGradeMappingEndpoint
{
    public static void MapUpdateGradeMappingEndpoint(this RouteGroupBuilder group)
    {
        group.MapPut("/{versionId:guid}/grade-mappings/{mappingId:guid}",
            async (Guid versionId, Guid mappingId, GradeMappingRequest body, UpdateGradeMappingHandler h, CancellationToken ct) =>
                (await h.Handle(new UpdateGradeMappingCommand(versionId, mappingId, body.GradeId, body.MinScore, body.MaxScore), ct)).ToHttp());
    }
}