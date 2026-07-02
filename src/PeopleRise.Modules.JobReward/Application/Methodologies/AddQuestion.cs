using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record AddQuestionCommand(Guid FactorId, string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, int SortOrder);

internal sealed class AddQuestionHandler(JobRewardDbContext db)
    : ICommandHandler<AddQuestionCommand, Result<QuestionDto>>
{
    public async Task<Result<QuestionDto>> Handle(AddQuestionCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.QuestionTextEn)) return Error.Validation("English question text is required.");
        if (!Enum.TryParse<QuestionType>(cmd.QuestionType, out var questionType))
            return Error.Validation("QuestionType must be SingleChoice or MultipleChoice.");

        var f = await db.Factors.FirstOrDefaultAsync(x => x.Id == cmd.FactorId, ct);
        if (f is null) return Error.NotFound("Factor not found.");

        var v = await db.MethodologyVersions.FirstAsync(x => x.Id == f.MethodologyVersionId, ct);
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        var q = Question.Create(cmd.FactorId, cmd.QuestionTextEn, cmd.QuestionTextAr, cmd.HelpTextEn, cmd.HelpTextAr, questionType, cmd.SortOrder);
        db.Questions.Add(q);
        await db.SaveChangesAsync(ct);
        return new QuestionDto(q.Id, q.QuestionTextEn, q.QuestionTextAr, q.HelpTextEn, q.HelpTextAr, q.QuestionType.ToString(), q.SortOrder);
    }
}
