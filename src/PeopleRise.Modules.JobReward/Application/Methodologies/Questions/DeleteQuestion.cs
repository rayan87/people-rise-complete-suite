using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Questions;

public sealed record DeleteQuestionCommand(Guid FactorId, Guid QuestionId);

internal sealed class DeleteQuestionHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteQuestionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteQuestionCommand cmd, CancellationToken ct)
    {
        var factor = await db.Factors
            .Include(f => f.MethodologyVersion)
            .Include(f => f.Questions)
            .Where(f => f.Id == cmd.FactorId)
            .FirstOrDefaultAsync(ct);

        if (factor is null)
        {
            return Error.NotFound("Factor not found.");
        }

        try 
        {
            var removed = factor.RemoveQuestion(cmd.QuestionId);

            if (!removed)
            {
                return Error.NotFound("Question not found");
            }
        } 
        catch (DomainStateException e) 
        { 
            return Error.Conflict(e.Message); 
        }
        catch (DomainException e)
        {
            return Error.Validation(e.Message);
        }

        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

internal static class DeleteQuestionEndpoint
{
    public static void MapDeleteQuestionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/factors/{factorId:guid}/questions/{questionId:guid}",
            async (Guid factorId, Guid questionId, DeleteQuestionHandler h, CancellationToken ct) =>
                (await h.Handle(new DeleteQuestionCommand(factorId, questionId), ct)).ToHttp());
    }
}
