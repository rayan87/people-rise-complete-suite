using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PeopleRise.Modules.JobReward.Domain;
using PeopleRise.Modules.JobReward.Infrastructure;
using PeopleRise.SharedKernel;

namespace PeopleRise.Modules.JobReward.Application.Evaluations;

public sealed record SubmitAnswersCommand(Guid EvaluationId, IReadOnlyList<AnswerSelection> Answers);

internal sealed class SubmitAnswersHandler(JobRewardDbContext db, ScoringService scoring)
    : ICommandHandler<SubmitAnswersCommand, Result<EvaluationResultDto>>
{
    public async Task<Result<EvaluationResultDto>> Handle(SubmitAnswersCommand cmd, CancellationToken ct)
    {
        var evaluation = await db.Evaluations
            .FirstOrDefaultAsync(e => e.Id == cmd.EvaluationId, ct);

        if (evaluation is null)
        {
            return Error.NotFound("Evaluation not found.");
        }

        if (evaluation.Status != EvaluationStatus.Draft)
        {
            return Error.Conflict(
                $"Evaluation is {evaluation.Status}; only a Draft can be submitted. Corrections create a NEW evaluation.");
        }
            
        var structure = await scoring.LoadStructureAsync(evaluation.MethodologyVersionId, ct);
        if (structure.Questions.Count == 0)
        {
            return Error.Validation("The pinned methodology version has no questions to score.");
        }
            
        var computed = ScoringService.Score(structure, cmd.Answers);
        if (computed.IsFailure)
        {
            return computed.Error!;
        }

        var score = computed.Value;

        var gradeId = await scoring.ResolveGradeIdAsync(evaluation.MethodologyVersionId, score.Total, ct);

        foreach (var a in score.Answers)
            db.EvaluationAnswers.Add(EvaluationAnswer.Create(evaluation.Id, a.QuestionId, a.AnswerOptionId, a.Points));
        foreach (var fs in score.FactorScores)
            db.EvaluationFactorScores.Add(EvaluationFactorScore.Create(evaluation.Id, fs.FactorId, fs.Score));

        evaluation.Submit(score.Total, gradeId);   // domain transition (eval is Draft, validated above)
        await db.SaveChangesAsync(ct);

        return (await EvaluationProjections.BuildAsync(db, evaluation.Id, ct))!;
    }
}

internal static class SubmitAnswersEndpoint
{
    public static void MapSubmitAnswersEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/answers",
            async (Guid id, SubmitAnswersRequest body, SubmitAnswersHandler h, CancellationToken ct) =>
                (await h.Handle(new SubmitAnswersCommand(id, body.Answers ?? []), ct)).ToHttp());
    }
}
