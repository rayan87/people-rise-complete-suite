using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Core.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Core.Application.Grades;

public sealed record DeleteGradeCommand(Guid Id);

// NOTE: this only checks core-side usage (Jobs). Before the core extraction this also checked
// JobReward's GradeMappings/SalaryBands/Evaluations tables, but the core must never read a module
// (LOCKED RULE 4) - Compensation/Job Evaluation now own protecting their own referenced grades
// (e.g. via a DB-level foreign key, or their own pre-check before a delete is attempted).
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

        var jobCount = await db.Jobs.CountAsync(j => j.GradeId == cmd.Id, ct);

        if (jobCount > 0)
        {
            return Error.Conflict($"Grade is in use by {jobCount} job(s) — reassign them before deleting.");
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
