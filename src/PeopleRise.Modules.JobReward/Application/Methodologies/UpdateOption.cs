using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record UpdateOptionCommand(Guid QuestionId, Guid OptionId, string LabelEn, string? LabelAr, int Points, int SortOrder);

internal sealed class UpdateOptionHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateOptionCommand, Result<AnswerOptionDto>>
{
    public async Task<Result<AnswerOptionDto>> Handle(UpdateOptionCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.LabelEn)) return Error.Validation("English label is required.");

        var o = await db.AnswerOptions.FirstOrDefaultAsync(x => x.Id == cmd.OptionId, ct);
        if (o is null || o.QuestionId != cmd.QuestionId) return Error.NotFound("Answer option not found.");

        var q = await db.Questions.FirstAsync(x => x.Id == o.QuestionId, ct);
        var f = await db.Factors.FirstAsync(x => x.Id == q.FactorId, ct);
        var v = await db.MethodologyVersions.FirstAsync(x => x.Id == f.MethodologyVersionId, ct);
        try { v.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        o.Update(cmd.LabelEn, cmd.LabelAr, cmd.Points, cmd.SortOrder);
        await db.SaveChangesAsync(ct);
        return new AnswerOptionDto(o.Id, o.LabelEn, o.LabelAr, o.Points, o.SortOrder);
    }
}
