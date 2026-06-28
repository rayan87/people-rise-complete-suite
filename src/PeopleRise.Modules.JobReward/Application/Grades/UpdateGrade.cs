using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Grades;

public sealed record UpdateGradeCommand(Guid Id, string Code, string NameEn, string? NameAr, int Rank, Guid? LevelId);

internal sealed class UpdateGradeHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateGradeCommand, Result<GradeDto>>
{
    public async Task<Result<GradeDto>> Handle(UpdateGradeCommand cmd, CancellationToken ct)
    {
        var grade = await db.Grades.FindAsync([cmd.Id], ct);
        if (grade is null) return Error.NotFound("Grade not found.");
        if (string.IsNullOrWhiteSpace(cmd.NameEn)) return Error.Validation("English name is required.");

        grade.Update(cmd.Code, cmd.NameEn, cmd.NameAr, cmd.Rank, cmd.LevelId);
        await db.SaveChangesAsync(ct);
        return new GradeDto(grade.Id, grade.Code, grade.NameEn, grade.NameAr, grade.Rank, grade.LevelId, null);
    }
}
