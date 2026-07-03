using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Application.Methodologies.Questions;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.AnswerOptions;

public sealed record DeleteOptionCommand(Guid QuestionId, Guid OptionId);

internal sealed class DeleteOptionHandler(JobRewardDbContext db)
    : ICommandHandler<DeleteOptionCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteOptionCommand cmd, CancellationToken ct)
    {
        var question = await db.Questions
            .Include(q => q.Factor!.MethodologyVersion)
            .Include(q => q.AnswerOptions)
            .Where(q => q.Id == cmd.QuestionId)
            .FirstOrDefaultAsync(ct);

        if (question is null)
        {
            return Error.NotFound("Question not found.");
        }

        try 
        {
            var removed = question.RemoveAnswerOption(cmd.OptionId);

            if (!removed)
            {
                return Error.NotFound("Answer option not found.");
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

internal static class DeleteAnswerOptionEndpoint
{
    public static void MapDeleteAnswerOptionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/questions/{questionId:guid}/options/{optionId:guid}",
            async (Guid questionId, Guid optionId, DeleteOptionHandler h, CancellationToken ct) =>
                (await h.Handle(new DeleteOptionCommand(questionId, optionId), ct)).ToHttp());
    }
}
