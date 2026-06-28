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
        var eval = await db.Evaluations.FirstOrDefaultAsync(e => e.Id == cmd.EvaluationId, ct);
        if (eval is null) return Error.NotFound("Evaluation not found.");
        if (eval.Status != EvaluationStatus.Draft)
            return Error.Conflict(
                $"Evaluation is {eval.Status}; only a Draft can be submitted. Corrections create a NEW evaluation.");

        var structure = await scoring.LoadStructureAsync(eval.MethodologyVersionId, ct);
        if (structure.Questions.Count == 0)
            return Error.Validation("The pinned methodology version has no questions to score.");

        var computed = ScoringService.Score(structure, cmd.Answers);
        if (computed.IsFailure) return computed.Error!;
        var score = computed.Value;

        var gradeId = await scoring.ResolveGradeIdAsync(eval.MethodologyVersionId, score.Total, ct);

        foreach (var a in score.Answers)
            db.EvaluationAnswers.Add(EvaluationAnswer.Create(eval.Id, a.QuestionId, a.AnswerOptionId, a.Points));
        foreach (var fs in score.FactorScores)
            db.EvaluationFactorScores.Add(EvaluationFactorScore.Create(eval.Id, fs.FactorId, fs.Score));

        eval.Submit(score.Total, gradeId);   // domain transition (eval is Draft, validated above)
        await db.SaveChangesAsync(ct);

        return (await EvaluationProjections.BuildAsync(db, eval.Id, ct))!;
    }
}
