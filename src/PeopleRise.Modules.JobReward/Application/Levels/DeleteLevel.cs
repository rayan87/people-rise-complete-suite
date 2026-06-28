using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Levels;

public sealed record DeleteLevelCommand(Guid Id);

internal sealed class DeleteLevelHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteLevelCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteLevelCommand cmd, CancellationToken ct)
    {
        var level = await db.Levels.FindAsync([cmd.Id], ct);
        if (level is null) return Error.NotFound("Level not found.");

        var jobCount   = await db.Jobs.CountAsync(j => j.LevelId == cmd.Id, ct);
        var gradeCount = await db.Grades.CountAsync(g => g.LevelId == cmd.Id, ct);
        if (jobCount > 0 || gradeCount > 0)
        {
            var parts = new List<string>();
            if (jobCount   > 0) parts.Add($"{jobCount} job(s)");
            if (gradeCount > 0) parts.Add($"{gradeCount} grade(s)");
            return Error.Conflict($"Level is in use by {string.Join(" and ", parts)} — reassign them before deleting.");
        }

        db.Levels.Remove(level);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
