using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record UpdateQuestionCommand(Guid FactorId, Guid QuestionId, string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, int SortOrder);

internal sealed class UpdateQuestionHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateQuestionCommand, Result<QuestionDto>>
{
    public async Task<Result<QuestionDto>> Handle(UpdateQuestionCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.QuestionTextEn)) return Error.Validation("English question text is required.");
        if (!Enum.TryParse<QuestionType>(cmd.QuestionType, out var questionType))
            return Error.Validation("QuestionType must be SingleChoice or MultipleChoice.");

        var q = await db.Questions.FirstOrDefaultAsync(x => x.Id == cmd.QuestionId, ct);
        if (q is null || q.FactorId != cmd.FactorId) return Error.NotFound("Question not found.");

        var f = await db.Factors.FirstAsync(x => x.Id == q.FactorId, ct);
        var v = await db.MethodologyVersions.FirstAsync(x => x.Id == f.MethodologyVersionId, ct);
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        q.Update(cmd.QuestionTextEn, cmd.QuestionTextAr, cmd.HelpTextEn, cmd.HelpTextAr, questionType, cmd.SortOrder);
        await db.SaveChangesAsync(ct);
        return new QuestionDto(q.Id, q.QuestionTextEn, q.QuestionTextAr, q.HelpTextEn, q.HelpTextAr, q.QuestionType.ToString(), q.SortOrder);
    }
}
