using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Grades;

public sealed record DeleteGradeCommand(Guid Id);

internal sealed class DeleteGradeHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteGradeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteGradeCommand cmd, CancellationToken ct)
    {
        var grade = await db.Grades.FindAsync(cmd.Id, ct);

        if (grade is null)
        {
            return Error.NotFound("Grade not found.");
        }

        var jobCount = await db.Jobs.CountAsync(j => j.GradeId == cmd.Id, ct);
        var mappingCount = await db.GradeMappings.CountAsync(m => m.GradeId == cmd.Id, ct);
        var bandCount = await db.SalaryBands.CountAsync(b => b.GradeId == cmd.Id, ct);
        var evalCount = await db.Evaluations.CountAsync(e => e.RecommendedGradeId == cmd.Id, ct);

        if (jobCount > 0 || mappingCount > 0 || bandCount > 0 || evalCount > 0)
        {
            var parts = new List<string>();
            if (jobCount > 0) parts.Add($"{jobCount} job(s)");
            if (mappingCount > 0) parts.Add($"{mappingCount} grade mapping(s)");
            if (bandCount > 0) parts.Add($"{bandCount} salary band(s)");
            if (evalCount > 0) parts.Add($"{evalCount} evaluation(s)");

            return Error.Conflict($"Grade is in use by {string.Join(", ", parts)} — reassign them before deleting.");
        }

        db.Grades.Remove(grade);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteGradeEndpoint
{
    public static void MapDeleteGradeEndpoint(this RouteGroupBuilder group)
    {
        group.MapDelete("/{id:guid}", async (Guid id, DeleteGradeHandler h, CancellationToken ct) =>
            (await h.Handle(new DeleteGradeCommand(id), ct)).ToHttp());
    }
}
