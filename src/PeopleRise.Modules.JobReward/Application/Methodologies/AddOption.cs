using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record AddOptionCommand(Guid QuestionId, string LabelEn, string? LabelAr, int Points, int SortOrder);

internal sealed class AddOptionHandler(JobRewardDbContext db)
    : ICommandHandler<AddOptionCommand, Result<AnswerOptionDto>>
{
    public async Task<Result<AnswerOptionDto>> Handle(AddOptionCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.LabelEn)) return Error.Validation("English label is required.");

        var q = await db.Questions.FirstOrDefaultAsync(x => x.Id == cmd.QuestionId, ct);
        if (q is null) return Error.NotFound("Question not found.");

        var f = await db.Factors.FirstAsync(x => x.Id == q.FactorId, ct);
        var v = await db.MethodologyVersions.FirstAsync(x => x.Id == f.MethodologyVersionId, ct);
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        var o = AnswerOption.Create(cmd.QuestionId, cmd.LabelEn, cmd.LabelAr, cmd.Points, cmd.SortOrder);
        db.AnswerOptions.Add(o);
        await db.SaveChangesAsync(ct);
        return new AnswerOptionDto(o.Id, o.LabelEn, o.LabelAr, o.Points, o.SortOrder);
    }
}
