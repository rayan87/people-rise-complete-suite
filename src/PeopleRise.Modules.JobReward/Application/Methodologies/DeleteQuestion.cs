using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies;

public sealed record DeleteQuestionCommand(Guid FactorId, Guid QuestionId);

internal sealed class DeleteQuestionHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteQuestionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteQuestionCommand cmd, CancellationToken ct)
    {
        var factor = await db.Factors
            .Include(f => f.MethodologyVersion)
            .FirstOrDefaultAsync(f => f.Id == cmd.FactorId, ct);
        if (factor is null) return Error.NotFound("Factor not found.");
        try { factor.MethodologyVersion!.EnsureEditable(); } catch (DomainStateException e) { return Error.Conflict(e.Message); }

        var question = await db.Questions
            .FirstOrDefaultAsync(q => q.Id == cmd.QuestionId && q.FactorId == cmd.FactorId, ct);
        if (question is null) return Error.NotFound("Question not found.");

        db.Questions.Remove(question);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
