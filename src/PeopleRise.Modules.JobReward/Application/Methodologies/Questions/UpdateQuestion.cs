using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Questions;

public sealed record UpdateQuestionCommand(Guid FactorId, Guid QuestionId, string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, decimal Weight, bool IsRequired, int SortOrder);

internal sealed class UpdateQuestionHandler(JobRewardDbContext db)
    : ICommandHandler<UpdateQuestionCommand, Result<QuestionDto>>
{
    public async Task<Result<QuestionDto>> Handle(UpdateQuestionCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.QuestionTextEn))
        {
            return Error.Validation("English question text is required.");
        }

        if (!Enum.TryParse<QuestionType>(cmd.QuestionType, out var questionType))
        {
            return Error.Validation("QuestionType must be SingleChoice or MultipleChoice.");
        }

        var factor = await db.Factors
            .Include(f => f.MethodologyVersion)
            .Include(f => f.Questions)
            .Where(f => f.Id == cmd.FactorId)
            .FirstOrDefaultAsync(ct);

        if (factor is null)
        {
            return Error.NotFound("Factor not found.");
        }

        Question? question;

        try
        {
            question = factor.UpdateQuestion(cmd.QuestionId,
                cmd.QuestionTextEn,
                cmd.QuestionTextAr,
                cmd.HelpTextEn,
                cmd.HelpTextAr,
                questionType,
                cmd.Weight,
                cmd.IsRequired,
                cmd.SortOrder);

            if (question is null)
            {
                return Error.NotFound("Question not found.");
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
        return new QuestionDto(question.Id,
            question.QuestionTextEn,
            question.QuestionTextAr,
            question.HelpTextEn,
            question.HelpTextAr,
            question.QuestionType.ToString(),
            question.Weight,
            question.IsRequired,
            question.SortOrder);
    }
}

internal static class UpdateQuestionEndpoint
{
    public static void MapUpdateQuestionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/factors/{factorId:guid}/questions/{questionId:guid}",
            async (Guid factorId, Guid questionId, QuestionRequest body, UpdateQuestionHandler h, CancellationToken ct) =>
                (await h.Handle(new UpdateQuestionCommand(factorId, questionId, body.QuestionTextEn, body.QuestionTextAr, body.HelpTextEn, body.HelpTextAr, body.QuestionType, body.Weight, body.IsRequired, body.SortOrder), ct)).ToHttp());
    }
}
