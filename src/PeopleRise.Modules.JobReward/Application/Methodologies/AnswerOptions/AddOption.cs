using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.AnswerOptions;

public sealed record AddOptionCommand(Guid QuestionId, string LabelEn, string? LabelAr, string? HelpTextEn, string? HelpTextAr, int Rating, int SortOrder);

internal sealed class AddOptionHandler(JobRewardDbContext db)
    : ICommandHandler<AddOptionCommand, Result<AnswerOptionDto>>
{
    public async Task<Result<AnswerOptionDto>> Handle(AddOptionCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.LabelEn))
        {
            return Error.Validation("English label is required.");
        }

        var question = await db.Questions
            .Include(q => q.Factor!.MethodologyVersion)
            .FirstOrDefaultAsync(x => x.Id == cmd.QuestionId, ct);

        if (question is null)
        {
            return Error.NotFound("Question not found.");
        }

        AnswerOption answerOption;

        try
        {
            answerOption = question.AddAnswerOption(cmd.LabelEn, cmd.LabelAr, cmd.HelpTextEn, cmd.HelpTextAr, cmd.Rating, cmd.SortOrder);
        }
        catch (DomainStateException e)
        {
            return Error.Conflict(e.Message);
        }
        catch (DomainException e)
        {
            return Error.Validation(e.Message);
        }

        db.AnswerOptions.Add(answerOption);
        await db.SaveChangesAsync(ct);

        var factorPoints = question.Factor!.MethodologyVersion!.MaxPoints * question.Factor.Weight / 100m;
        var questionPoints = factorPoints * question.Weight / 100m;
        var calculatedPoints = (int)Math.Round(questionPoints * answerOption.Rating / 5m, MidpointRounding.AwayFromZero);

        return new AnswerOptionDto(answerOption.Id,
            answerOption.LabelEn,
            answerOption.LabelAr,
            answerOption.HelpTextEn,
            answerOption.HelpTextAr,
            answerOption.Rating,
            answerOption.SortOrder,
            calculatedPoints);
    }
}

internal static class AddAnswerOptionEndpoint
{
    public static void MapAddAnswerOptionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/questions/{questionId:guid}/options",
            async (Guid questionId, AnswerOptionRequest body, AddOptionHandler h, CancellationToken ct) =>
                (await h.Handle(new AddOptionCommand(questionId, body.LabelEn, body.LabelAr, body.HelpTextEn, body.HelpTextAr, body.Rating, body.SortOrder), ct)).ToHttp());
    }
}
