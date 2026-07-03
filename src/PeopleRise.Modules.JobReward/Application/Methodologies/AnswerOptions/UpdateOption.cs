using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.AnswerOptions;

public sealed record UpdateOptionCommand(Guid QuestionId, Guid OptionId, string LabelEn, string? LabelAr, int Points, int SortOrder);

internal sealed class UpdateOptionHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateOptionCommand, Result<AnswerOptionDto>>
{
    public async Task<Result<AnswerOptionDto>> Handle(UpdateOptionCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.LabelEn))
        {
            return Error.Validation("English label is required.");
        }

        var question = await db.Questions
            .Include(q => q.Factor!.MethodologyVersion)
            .Include(q => q.AnswerOptions)
            .Where(q => q.Id == cmd.QuestionId)
            .FirstOrDefaultAsync(ct);

        if (question is null)
        {
            return Error.NotFound("Question not found.");
        }

        AnswerOption? option;

        try 
        {
            option = question.UpdateAnswerOption(cmd.OptionId, cmd.LabelEn, cmd.LabelAr, cmd.Points, cmd.SortOrder);

            if (option is null)
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
        return new AnswerOptionDto(option.Id, 
            option.LabelEn, 
            option.LabelAr, 
            option.Points, 
            option.SortOrder);
    }
}

internal static class UpdateAnswerOptionEndpoint
{
    public static void MapUpdateAnswerOptionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/questions/{questionId:guid}/options/{optionId:guid}",
            async (Guid questionId, Guid optionId, AnswerOptionRequest body, UpdateOptionHandler h, CancellationToken ct) =>
                (await h.Handle(new UpdateOptionCommand(questionId, optionId, body.LabelEn, body.LabelAr, body.Points, body.SortOrder), ct)).ToHttp());
    }
}
