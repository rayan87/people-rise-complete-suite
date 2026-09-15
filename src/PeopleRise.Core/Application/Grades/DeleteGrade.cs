using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Grades;

public sealed record DeleteGradeCommand(Guid Id);

// Closed, never deleted, once a job has ever been assigned to it (Core Spec §6) - checked against
// the full JobGradeAssignment history, not just the current one, since that's what closure
// protects. A band referencing the grade closes it too (§3.4's general rule: anything ever
// referenced is closed). It still can't see JobReward's GradeMappings/Evaluations tables (the core
// must never read a module - LOCKED RULE 4); Job Evaluation owns protecting its own referenced
// grades there.
internal sealed class DeleteGradeHandler(CoreDbContext db)
    : ICommandHandler<DeleteGradeCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteGradeCommand cmd, CancellationToken ct)
    {
        var grade = await db.Grades.FindAsync(cmd.Id, ct);

        if (grade is null)
        {
            return Error.NotFound("Grade not found.");
        }

        var everReferenced = await db.JobGradeAssignments.AnyAsync(a => a.GradeId == cmd.Id, ct)
            || await db.SalaryBands.AnyAsync(b => b.GradeId == cmd.Id, ct);

        if (everReferenced)
        {
            grade.Close();
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
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
