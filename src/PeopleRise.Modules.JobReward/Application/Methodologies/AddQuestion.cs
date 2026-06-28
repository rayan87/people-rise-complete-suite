using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record AddQuestionCommand(Guid FactorId, string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, int SortOrder);

internal sealed class AddQuestionHandler(JobRewardDbContext db)
    : ICommandHandler<AddQuestionCommand, Result<QuestionDto>>
{
    public async Task<Result<QuestionDto>> Handle(AddQuestionCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.QuestionTextEn)) return Error.Validation("English question text is required.");

        var f = await db.Factors.FirstOrDefaultAsync(x => x.Id == cmd.FactorId, ct);
        if (f is null) return Error.NotFound("Factor not found.");

        var v = await db.MethodologyVersions.FirstAsync(x => x.Id == f.MethodologyVersionId, ct);
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        var q = Question.Create(cmd.FactorId, cmd.QuestionTextEn, cmd.QuestionTextAr, cmd.HelpTextEn, cmd.HelpTextAr, cmd.SortOrder);
        db.Questions.Add(q);
        await db.SaveChangesAsync(ct);
        return new QuestionDto(q.Id, q.QuestionTextEn, q.QuestionTextAr, q.HelpTextEn, q.HelpTextAr, q.SortOrder);
    }
}
