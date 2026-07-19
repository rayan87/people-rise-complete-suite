using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Application.Methodologies.GradeMappings;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Methodologies.Questions;

public sealed record AddQuestionCommand(Guid FactorId, string QuestionTextEn, string? QuestionTextAr, string? HelpTextEn, string? HelpTextAr, string QuestionType, decimal Weight, bool IsRequired, int SortOrder);

internal sealed class AddQuestionHandler(JobRewardDbContext db)
    : ICommandHandler<AddQuestionCommand, Result<QuestionDto>>
{
    public async Task<Result<QuestionDto>> Handle(AddQuestionCommand cmd, CancellationToken ct)
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
            .Where(f => f.Id == cmd.FactorId)
            .FirstOrDefaultAsync(ct);

        if (factor is null)
        {
            return Error.NotFound("Factor not found.");
        }

        Question question;

        try
        {
            question = factor.AddQuestion(cmd.QuestionTextEn,
                cmd.QuestionTextAr,
                cmd.HelpTextEn,
                cmd.HelpTextAr,
                questionType,
                cmd.Weight,
                cmd.IsRequired,
                cmd.SortOrder);
        }
        catch (DomainStateException e)
        {
            return Error.Conflict(e.Message);
        }
        catch (DomainException e)
        {
            return Error.Validation(e.Message);
        }

        db.Questions.Add(question);
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

internal static class AddQuestionEndpoint
{
    public static void MapAddQuestionEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/factors/{factorId:guid}/questions",
            async (Guid factorId, QuestionRequest body, AddQuestionHandler h, CancellationToken ct) =>
                (await h.Handle(new AddQuestionCommand(factorId, body.QuestionTextEn, body.QuestionTextAr, body.HelpTextEn, body.HelpTextAr, body.QuestionType, body.Weight, body.IsRequired, body.SortOrder), ct)).ToHttp());
    }
}
