using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record DeleteOptionCommand(Guid QuestionId, Guid OptionId);

internal sealed class DeleteOptionHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteOptionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteOptionCommand cmd, CancellationToken ct)
    {
        var question = await db.Questions
            .Include(q => q.Factor).ThenInclude(f => f!.MethodologyVersion)
            .FirstOrDefaultAsync(q => q.Id == cmd.QuestionId, ct);
        if (question is null) return Error.NotFound("Question not found.");
        try { question.Factor!.MethodologyVersion!.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        var option = await db.AnswerOptions
            .FirstOrDefaultAsync(o => o.Id == cmd.OptionId && o.QuestionId == cmd.QuestionId, ct);
        if (option is null) return Error.NotFound("Answer option not found.");

        db.AnswerOptions.Remove(option);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
